from ultralytics import YOLO
import cv2
import torch
import numpy as np
from collections import deque, Counter
import mediapipe as mp
from deepface import DeepFace
import math
from scipy.optimize import linear_sum_assignment
from scipy.spatial.distance import cosine
from scipy.spatial import distance as dist

# Cihaz seçimi
device = torch.device("mps" if torch.backends.mps.is_available() else "cpu")

# Model boyutu seçimi (değiştirilebilir)
# n: en hızlı (60+ FPS), s: hızlı (40+ FPS), m: dengeli (25+ FPS), l: yavaş (15+ FPS), x: en yavaş (10+ FPS)
MODEL_SIZE = "m"  # MacBook Air M3 için m veya l öneriyorum

# YOLO modelini yükle - M3 için optimize
model_size = MODEL_SIZE  # Global değişken

try:
    # Önce face-specific model dene
    yolo_model = YOLO(f"yolov8{model_size}-face.pt")
    print(f"YOLOv8{model_size}-face modeli yüklendi")
except:
    # Face modeli yoksa standart model kullan
    print(f"Face modeli bulunamadı, standart YOLOv8{model_size} kullanılıyor...")
    yolo_model = YOLO(f"yolov8{model_size}.pt")  # Otomatik indirir

print(f"Model boyutu: {model_size} - MacBook Air M3 için optimize")

# MediaPipe Face Mesh kurulumu
mp_face_mesh = mp.solutions.face_mesh
face_mesh = mp_face_mesh.FaceMesh(
    static_image_mode=False,
    max_num_faces=10,  # 10 kişilik sınıf için
    refine_landmarks=True,
    min_detection_confidence=0.2,  # Düşürüldü (önceki: 0.5)
    min_tracking_confidence=0.2  # Düşürüldü (önceki: 0.5)
)


# Emotion Tracking için yardımcı sınıf
class EmotionTracker:
    def __init__(self, face_id, smoothing_window=10):
        self.face_id = face_id
        self.emotion_scores = {
            'angry': deque(maxlen=smoothing_window),
            'disgust': deque(maxlen=smoothing_window),
            'fear': deque(maxlen=smoothing_window),
            'happy': deque(maxlen=smoothing_window),
            'sad': deque(maxlen=smoothing_window),
            'surprise': deque(maxlen=smoothing_window),
            'neutral': deque(maxlen=smoothing_window)
        }
        self.last_valid_emotion = 'neutral'
        self.emotion_change_count = 0
        self.last_dominant_emotion = 'neutral'

        # Göz takibi için
        self.eye_closed_frames = 0
        self.blink_count = 0
        self.last_eye_state = "open"
        self.eye_aspect_ratios = deque(maxlen=5)  # Smoothing için
        self.eye_state_buffer = deque(maxlen=3)  # Göz durumu smoothing

    def update(self, emotion_result):
        """DeepFace'ten gelen tüm skorları sakla"""
        if isinstance(emotion_result, dict):
            # Eğer emotion_result doğrudan skorları içeriyorsa
            for emotion in self.emotion_scores.keys():
                if emotion in emotion_result:
                    self.emotion_scores[emotion].append(emotion_result[emotion])
        elif isinstance(emotion_result, str):
            # Sadece dominant emotion geliyorsa
            for emotion in self.emotion_scores.keys():
                if emotion == emotion_result:
                    self.emotion_scores[emotion].append(100)
                else:
                    self.emotion_scores[emotion].append(0)

    def get_stable_emotion(self):
        """Weighted average ile daha stabil emotion döndür"""
        avg_scores = {}

        for emotion, scores in self.emotion_scores.items():
            if scores:
                # Son skorlara daha fazla ağırlık ver
                weights = np.linspace(0.5, 1.0, len(scores))
                avg_scores[emotion] = np.average(list(scores), weights=weights)
            else:
                avg_scores[emotion] = 0

        if avg_scores:
            stable_emotion = max(avg_scores, key=avg_scores.get)
            confidence = avg_scores[stable_emotion] / 100.0

            # Emotion değişimini takip et
            if stable_emotion != self.last_dominant_emotion:
                self.emotion_change_count += 1
                self.last_dominant_emotion = stable_emotion

            self.last_valid_emotion = stable_emotion
            return stable_emotion, confidence

        return self.last_valid_emotion, 0


# İyileştirilmiş Face Tracking sınıfı
class FaceTracker:
    def __init__(self, max_disappeared=30, max_distance=150, iou_threshold=0.3):
        self.next_id = 0
        self.faces = {}
        self.disappeared = {}
        self.max_disappeared = max_disappeared
        self.max_distance = max_distance
        self.iou_threshold = iou_threshold

        # ID koruması için ek veri yapıları
        self.face_features = {}  # Yüz özelliklerini sakla
        self.id_history = {}  # Her ID'nin geçmiş konumları
        self.emotion_trackers = {}  # Her yüz için emotion tracker
        self.face_embeddings = {}  # Face embeddings for re-identification
        self.embedding_threshold = 0.6

    def register(self, detection):
        """Yeni yüz kaydet"""
        face_id = self.next_id

        self.faces[face_id] = {
            'bbox': detection['bbox'],
            'centroid': self._calculate_centroid(detection['bbox']),
            'emotion_history': deque(maxlen=10),
            'head_turn_count': 0,
            'last_yaw': detection.get('yaw', 0),
            'frame_count': 0,
            'confidence': 1.0,
            'stable_frames': 0,
            'eye_closed_count': 0,  # Göz kapalı toplam frame sayısı
            'blink_count': 0,  # Göz kırpma sayısı
            'last_eye_state': 'open'  # Son göz durumu
        }

        # Emotion tracker başlat
        self.emotion_trackers[face_id] = EmotionTracker(face_id)

        # İlk emotion'ı ekle
        if 'emotion' in detection:
            self.faces[face_id]['emotion_history'].append(detection['emotion'])

        self.disappeared[face_id] = 0
        self.face_features[face_id] = detection.get('features', None)
        self.id_history[face_id] = deque(maxlen=30)
        self.id_history[face_id].append(self.faces[face_id]['centroid'])

        # Embedding varsa sakla
        if 'embedding' in detection:
            self.face_embeddings[face_id] = detection['embedding']

        self.next_id += 1
        return face_id

    def deregister(self, face_id):
        """Yüzü sil"""
        if face_id in self.faces:
            del self.faces[face_id]
        if face_id in self.disappeared:
            del self.disappeared[face_id]
        if face_id in self.face_features:
            del self.face_features[face_id]
        if face_id in self.id_history:
            del self.id_history[face_id]
        if face_id in self.emotion_trackers:
            del self.emotion_trackers[face_id]
        # Embedding'i sakla (re-identification için)
        # if face_id in self.face_embeddings:
        #     del self.face_embeddings[face_id]

    def find_similar_face(self, new_embedding):
        """Kayıp bir yüzü embedding'e göre bul"""
        if new_embedding is None:
            return None

        best_match_id = None
        best_similarity = -1

        for face_id, stored_embedding in self.face_embeddings.items():
            if face_id not in self.faces:  # Sadece kayıp yüzlerde ara
                try:
                    similarity = 1 - cosine(new_embedding.flatten(), stored_embedding.flatten())
                    if similarity > self.embedding_threshold and similarity > best_similarity:
                        best_similarity = similarity
                        best_match_id = face_id
                except:
                    continue

        return best_match_id if best_similarity > self.embedding_threshold else None

    def _calculate_centroid(self, bbox):
        """Bbox'tan merkez noktayı hesapla"""
        x1, y1, x2, y2 = bbox
        return ((x1 + x2) // 2, (y1 + y2) // 2)

    def _calculate_iou(self, box1, box2):
        """İki bbox arasındaki IoU hesapla"""
        x1_1, y1_1, x2_1, y2_1 = box1
        x1_2, y1_2, x2_2, y2_2 = box2

        # Kesişim alanı
        x1_i = max(x1_1, x1_2)
        y1_i = max(y1_1, y1_2)
        x2_i = min(x2_1, x2_2)
        y2_i = min(y2_1, y2_2)

        if x2_i < x1_i or y2_i < y1_i:
            return 0.0

        intersection = (x2_i - x1_i) * (y2_i - y1_i)

        # Birleşim alanı
        area1 = (x2_1 - x1_1) * (y2_1 - y1_1)
        area2 = (x2_2 - x1_2) * (y2_2 - y1_2)
        union = area1 + area2 - intersection

        return intersection / union if union > 0 else 0

    def _calculate_distance_score(self, face_id, detection):
        """Mesafe + IoU + hareket tahmini bazlı skor hesapla"""
        face_data = self.faces[face_id]

        # 1. Centroid mesafesi
        centroid1 = face_data['centroid']
        centroid2 = self._calculate_centroid(detection['bbox'])
        distance = math.sqrt((centroid1[0] - centroid2[0]) ** 2 +
                             (centroid1[1] - centroid2[1]) ** 2)

        # 2. IoU skoru
        iou = self._calculate_iou(face_data['bbox'], detection['bbox'])

        # 3. Hareket tahmini (eğer history varsa)
        motion_penalty = 0
        if face_id in self.id_history and len(self.id_history[face_id]) > 2:
            # Son hareket vektörünü hesapla
            history = list(self.id_history[face_id])
            last_motion = (history[-1][0] - history[-2][0],
                           history[-1][1] - history[-2][1])

            # Beklenen pozisyon
            expected_pos = (history[-1][0] + last_motion[0],
                            history[-1][1] + last_motion[1])

            # Gerçek pozisyonla fark
            motion_error = math.sqrt((expected_pos[0] - centroid2[0]) ** 2 +
                                     (expected_pos[1] - centroid2[1]) ** 2)
            motion_penalty = motion_error * 0.5

        # Kombinasyon skoru (düşük = iyi)
        score = distance + motion_penalty - (iou * 200)  # IoU'yu ödüllendir

        return score

    def update(self, detections):
        """Tespit edilen yüzleri güncelle - Hungarian algorithm ile"""

        # Hiç detection yoksa
        if len(detections) == 0:
            for face_id in list(self.disappeared.keys()):
                self.disappeared[face_id] += 1
                if self.disappeared[face_id] > self.max_disappeared:
                    self.deregister(face_id)
            return self.faces

        # İlk frame veya hiç face yoksa
        if len(self.faces) == 0:
            for detection in detections:
                self.register(detection)
            return self.faces

        # Cost matrix oluştur
        face_ids = list(self.faces.keys())
        n_faces = len(face_ids)
        n_detections = len(detections)

        cost_matrix = np.full((n_faces, n_detections), 1e6)  # Yüksek başlangıç değeri

        # Her face-detection çifti için skor hesapla
        for i, face_id in enumerate(face_ids):
            for j, detection in enumerate(detections):
                score = self._calculate_distance_score(face_id, detection)

                # Eğer çok uzaksa, bu eşleştirmeyi yapma
                distance = math.sqrt(
                    (self.faces[face_id]['centroid'][0] - self._calculate_centroid(detection['bbox'])[0]) ** 2 +
                    (self.faces[face_id]['centroid'][1] - self._calculate_centroid(detection['bbox'])[1]) ** 2
                )

                if distance < self.max_distance:
                    cost_matrix[i, j] = score

        # Hungarian algorithm ile optimal eşleştirme
        if n_faces > 0 and n_detections > 0:
            row_indices, col_indices = linear_sum_assignment(cost_matrix)

            used_face_indices = set()
            used_detection_indices = set()

            # Eşleştirmeleri uygula
            for row_idx, col_idx in zip(row_indices, col_indices):
                if cost_matrix[row_idx, col_idx] < 1e5:  # Geçerli eşleştirme
                    face_id = face_ids[row_idx]
                    detection = detections[col_idx]

                    # Güncelleme yap
                    old_centroid = self.faces[face_id]['centroid']
                    new_centroid = self._calculate_centroid(detection['bbox'])

                    # Smooth update (ani sıçramaları önle)
                    alpha = 0.7  # Smoothing faktörü
                    smoothed_centroid = (
                        int(alpha * new_centroid[0] + (1 - alpha) * old_centroid[0]),
                        int(alpha * new_centroid[1] + (1 - alpha) * old_centroid[1])
                    )

                    self.faces[face_id]['bbox'] = detection['bbox']
                    self.faces[face_id]['centroid'] = smoothed_centroid
                    self.faces[face_id]['frame_count'] += 1
                    self.disappeared[face_id] = 0

                    # Emotion güncelleme
                    if 'emotion' in detection:
                        self.faces[face_id]['emotion_history'].append(detection['emotion'])
                        # Emotion tracker'a da gönder
                        if face_id in self.emotion_trackers:
                            self.emotion_trackers[face_id].update(detection['emotion'])

                    if 'emotion_scores' in detection and face_id in self.emotion_trackers:
                        self.emotion_trackers[face_id].update(detection['emotion_scores'])

                    # Kafa dönüşü kontrolü
                    if 'yaw' in detection:
                        current_yaw = detection['yaw']
                        last_yaw = self.faces[face_id]['last_yaw']

                        # Büyük yaw değişimi (20 dereceden fazla)
                        if abs(current_yaw - last_yaw) > 20:
                            self.faces[face_id]['head_turn_count'] += 1
                            self.faces[face_id]['stable_frames'] = 0
                        else:
                            self.faces[face_id]['stable_frames'] += 1

                        self.faces[face_id]['last_yaw'] = current_yaw

                    # Güvenilirlik güncelle
                    self.faces[face_id]['confidence'] = min(1.0,
                                                            self.faces[face_id]['confidence'] + 0.1)

                    # History güncelle
                    self.id_history[face_id].append(smoothed_centroid)

                    # Embedding güncelle
                    if 'embedding' in detection:
                        self.face_embeddings[face_id] = detection['embedding']

                    # Göz durumu güncelle (smoothing ile)
                    if 'eye_state' in detection:
                        current_eye_state = detection['eye_state']

                        # Smoothing için buffer'a ekle
                        if face_id in self.emotion_trackers:
                            self.emotion_trackers[face_id].eye_state_buffer.append(current_eye_state)

                            # Çoğunluk oyu ile göz durumunu belirle
                            if len(self.emotion_trackers[face_id].eye_state_buffer) >= 2:
                                closed_count = sum(
                                    1 for state in self.emotion_trackers[face_id].eye_state_buffer if state == 'closed')
                                if closed_count >= 2:
                                    smoothed_eye_state = 'closed'
                                else:
                                    smoothed_eye_state = 'open'
                            else:
                                smoothed_eye_state = current_eye_state
                        else:
                            smoothed_eye_state = current_eye_state

                        last_eye_state = self.faces[face_id]['last_eye_state']

                        # Göz kapalıysa sayacı artır
                        if smoothed_eye_state == 'closed':
                            self.faces[face_id]['eye_closed_count'] += 1
                            if face_id in self.emotion_trackers:
                                self.emotion_trackers[face_id].eye_closed_frames += 1

                        # Göz kırpma tespiti (open -> closed -> open)
                        if last_eye_state == 'open' and smoothed_eye_state == 'closed':
                            # Göz kapanmaya başladı
                            pass
                        elif last_eye_state == 'closed' and smoothed_eye_state == 'open':
                            # Göz açıldı - bu bir göz kırpma
                            self.faces[face_id]['blink_count'] += 1
                            if face_id in self.emotion_trackers:
                                self.emotion_trackers[face_id].blink_count += 1

                        self.faces[face_id]['last_eye_state'] = smoothed_eye_state

                        # EAR değerini sakla
                        if 'ear' in detection and face_id in self.emotion_trackers:
                            self.emotion_trackers[face_id].eye_aspect_ratios.append(detection['ear'])

                    used_face_indices.add(row_idx)
                    used_detection_indices.add(col_idx)

            # Eşleşmeyen face'ler için disappeared artır
            for i, face_id in enumerate(face_ids):
                if i not in used_face_indices:
                    self.disappeared[face_id] += 1
                    self.faces[face_id]['confidence'] *= 0.9  # Güvenilirliği azalt

                    if self.disappeared[face_id] > self.max_disappeared:
                        self.deregister(face_id)

            # Eşleşmeyen detection'lar için
            for j, detection in enumerate(detections):
                if j not in used_detection_indices:
                    # Önce embedding ile eski yüz var mı kontrol et
                    if 'embedding' in detection:
                        similar_id = self.find_similar_face(detection['embedding'])
                        if similar_id is not None:
                            # Eski yüzü geri getir
                            self.faces[similar_id] = {
                                'bbox': detection['bbox'],
                                'centroid': self._calculate_centroid(detection['bbox']),
                                'emotion_history': self.faces.get(similar_id, {}).get('emotion_history',
                                                                                      deque(maxlen=10)),
                                'head_turn_count': self.faces.get(similar_id, {}).get('head_turn_count', 0),
                                'last_yaw': detection.get('yaw', 0),
                                'frame_count': self.faces.get(similar_id, {}).get('frame_count', 0) + 1,
                                'confidence': 0.5,  # Düşük güvenle başla
                                'stable_frames': 0
                            }
                            self.disappeared[similar_id] = 0
                            continue

                    # Yeni yüz olarak kaydet
                    self.register(detection)

        return self.faces


def extract_face_embedding(face_crop):
    """Face embedding çıkar (re-identification için)"""
    try:
        if face_crop is None or face_crop.shape[0] < 48 or face_crop.shape[1] < 48:
            return None

        # DeepFace ile embedding al
        embedding = DeepFace.represent(
            face_crop,
            model_name="Facenet512",  # Daha küçük model
            enforce_detection=False
        )

        if isinstance(embedding, list) and len(embedding) > 0:
            return np.array(embedding[0]["embedding"])
        return None
    except:
        return None


def predict_emotion(face_crop):
    """Duygu tahmini - basitleştirilmiş"""
    try:
        # Çok küçük yüzleri atla
        if face_crop.shape[0] < 48 or face_crop.shape[1] < 48:
            return "unknown", {}

        rgb_face = cv2.cvtColor(face_crop, cv2.COLOR_BGR2RGB)
        result = DeepFace.analyze(
            rgb_face,
            actions=['emotion'],
            enforce_detection=False
        )

        if isinstance(result, list):
            result = result[0]

        dominant_emotion = result.get('dominant_emotion', 'unknown')
        emotion_scores = result.get('emotion', {})

        return dominant_emotion, emotion_scores
    except Exception as e:
        return "unknown", {}


def calculate_ear(eye_landmarks):
    """Eye Aspect Ratio (EAR) hesapla"""
    try:
        # Göz landmark'ları arasındaki dikey mesafeler
        A = dist.euclidean(eye_landmarks[1], eye_landmarks[5])
        B = dist.euclidean(eye_landmarks[2], eye_landmarks[4])

        # Göz landmark'ları arasındaki yatay mesafe
        C = dist.euclidean(eye_landmarks[0], eye_landmarks[3])

        # Sıfıra bölmeyi önle
        if C == 0:
            return 0.3  # Default açık göz değeri

        # EAR hesapla
        ear = (A + B) / (2.0 * C)
        return ear
    except:
        return 0.3  # Hata durumunda default

def calculate_adaptive_ear_threshold(face_width, base_threshold=0.22, min_width=40, max_width=200):
    """
    Yüz genişliğine göre EAR threshold değerini ayarlar.
    face_width: Yüz genişliği (piksel)
    base_threshold: Temel EAR threshold (örn. 0.22)
    min_width: Minimum yüz genişliği için referans
    max_width: Maksimum yüz genişliği için referans
    """
    if face_width < min_width:
        return base_threshold + 0.1
    elif face_width > max_width:
        return base_threshold - 0.05
    else:
        ratio = (face_width - min_width) / (max_width - min_width)
        return base_threshold + 0.1 - ratio * 0.15


def get_eye_landmarks(landmarks_2d):
    """MediaPipe landmark'larından göz koordinatlarını al - İyileştirilmiş"""
    # Daha geniş göz bölgesi için alternatif indeksler
    # Sol göz - daha fazla nokta
    LEFT_EYE_INDEXES = [362, 385, 387, 263, 373, 380]
    LEFT_EYE_ALT = [33, 246, 161, 160, 159, 158, 157, 173]  # Alternatif

    # Sağ göz - daha fazla nokta
    RIGHT_EYE_INDEXES = [33, 160, 158, 133, 153, 144]
    RIGHT_EYE_ALT = [362, 398, 384, 385, 386, 387, 388, 466]  # Alternatif

    try:
        left_eye = [landmarks_2d[i] for i in LEFT_EYE_INDEXES]
        right_eye = [landmarks_2d[i] for i in RIGHT_EYE_INDEXES]
    except:
        # Alternatif indeksleri dene
        try:
            left_eye = [landmarks_2d[i] for i in LEFT_EYE_ALT[:6]]
            right_eye = [landmarks_2d[i] for i in RIGHT_EYE_ALT[:6]]
        except:
            return None, None

    return left_eye, right_eye


def enhance_face_for_eye_detection(face_crop):
    """Küçük/bulanık yüzleri göz tespiti için iyileştir"""
    try:
        # 1. Minimum boyut kontrolü
        if face_crop.shape[0] < 64 or face_crop.shape[1] < 64:
            # Küçük yüzleri büyüt
            face_crop = cv2.resize(face_crop, (128, 128), interpolation=cv2.INTER_CUBIC)

        # 2. Keskinleştirme filtresi
        kernel = np.array([[-1, -1, -1],
                           [-1, 9, -1],
                           [-1, -1, -1]])
        face_crop = cv2.filter2D(face_crop, -1, kernel)

        # 3. CLAHE (Contrast Limited Adaptive Histogram Equalization)
        gray = cv2.cvtColor(face_crop, cv2.COLOR_BGR2GRAY)
        clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
        enhanced_gray = clahe.apply(gray)

        # Geri BGR'ye çevir
        face_crop = cv2.cvtColor(enhanced_gray, cv2.COLOR_GRAY2BGR)

        return face_crop
    except:
        return face_crop


def calculate_eye_darkness_ratio(face_crop, eye_region):
    """Göz bölgesinin karanlık oranını hesapla - MediaPipe alternatifi"""
    try:
        gray = cv2.cvtColor(face_crop, cv2.COLOR_BGR2GRAY)
        h, w = gray.shape

        # Göz bölgesi tahminleri (yüzün üst yarısı)
        if eye_region == "both":
            # Üst 1/3 bölge (her iki göz)
            eye_area = gray[int(h * 0.2):int(h * 0.45), :]
        elif eye_region == "left":
            eye_area = gray[int(h * 0.2):int(h * 0.45), int(w * 0.55):]
        else:  # right
            eye_area = gray[int(h * 0.2):int(h * 0.45), :int(w * 0.45)]

        # Ortalama parlaklık
        mean_brightness = np.mean(eye_area)

        # Karanlık piksel oranı (threshold: 50)
        dark_pixels = np.sum(eye_area < 50)
        total_pixels = eye_area.size
        darkness_ratio = dark_pixels / total_pixels

        return darkness_ratio, mean_brightness
    except:
        return 0, 255


def detect_eye_state_alternative(face_crop, ear_value=None):
    """EAR başarısız olduğunda alternatif göz durumu tespiti"""
    # 1. Göz bölgesi karanlık oranı
    darkness_ratio, brightness = calculate_eye_darkness_ratio(face_crop, "both")

    # 2. Karar verme
    if ear_value is not None and ear_value < 0.25:
        # EAR düşükse ve karanlık oran yüksekse -> kapalı
        if darkness_ratio > 0.4:
            return "closed", 0.8  # confidence
        elif darkness_ratio > 0.3:
            return "possibly_closed", 0.5

    # Sadece karanlık orana göre
    if darkness_ratio > 0.5 and brightness < 60:
        return "closed", 0.6
    elif darkness_ratio > 0.35 and brightness < 80:
        return "possibly_closed", 0.4

    return "open", 0.7


def landmarks_to_np(landmarks, frame_width, frame_height):
    """MediaPipe landmark'larını numpy array'e çevir"""
    coords = []
    for lm in landmarks.landmark:
        x, y = int(lm.x * frame_width), int(lm.y * frame_height)
        coords.append((x, y))
    return coords


def get_head_pose(landmarks_2d, frame_size):
    """Kafa pozisyonu hesapla"""
    try:
        model_points = np.array([
            (0.0, 0.0, 0.0),  # Burun ucu
            (0.0, -330.0, -65.0),  # Çene
            (-225.0, 170.0, -135.0),  # Sol göz köşesi
            (225.0, 170.0, -135.0),  # Sağ göz köşesi
            (-150.0, -150.0, -125.0),  # Sol ağız köşesi
            (150.0, -150.0, -125.0)  # Sağ ağız köşesi
        ])

        image_points = np.array([
            landmarks_2d[1],  # Burun ucu
            landmarks_2d[152],  # Çene
            landmarks_2d[263],  # Sol göz köşesi
            landmarks_2d[33],  # Sağ göz köşesi
            landmarks_2d[287],  # Sol ağız köşesi
            landmarks_2d[57]  # Sağ ağız köşesi
        ], dtype="double")

        focal_length = frame_size[1]
        center = (frame_size[1] / 2, frame_size[0] / 2)
        camera_matrix = np.array(
            [[focal_length, 0, center[0]],
             [0, focal_length, center[1]],
             [0, 0, 1]], dtype="double"
        )
        dist_coeffs = np.zeros((4, 1))

        success, rotation_vector, translation_vector = cv2.solvePnP(
            model_points, image_points, camera_matrix, dist_coeffs, flags=cv2.SOLVEPNP_ITERATIVE
        )

        rmat, _ = cv2.Rodrigues(rotation_vector)
        pose_mat = cv2.hconcat([rmat, translation_vector])
        _, _, _, _, _, _, euler_angles = cv2.decomposeProjectionMatrix(pose_mat)

        yaw = euler_angles[1, 0]
        pitch = euler_angles[0, 0]
        roll = euler_angles[2, 0]

        return yaw, pitch, roll
    except:
        return 0, 0, 0


# Ana program
if __name__ == "__main__":
    # Face tracker'ı başlat
    face_tracker = FaceTracker(max_disappeared=30, max_distance=120, iou_threshold=0.3)

    # Video kaynağı - komut satırı argümanı veya webcam
    import sys

    video_source = 0  # Default webcam

    if len(sys.argv) > 1:
        video_source = sys.argv[1]
        print(f"Video dosyası yükleniyor: {video_source}")
        # Video dosyaları için özel ayarlar
        EYE_AR_THRESH = 0.18  # Sınıf videoları için daha düşük
    else:
        # BURAYA VİDEO YOLUNU YAZABİLİRSİNİZ
        # video_source = "video.mp4"  # Aynı klasördeki video
        # video_source = r"C:\Users\kullanici\Videos\sinif_kaydi.mp4"  # Windows
        # video_source = "/Users/kullanici/Desktop/ders.mp4"  # Mac/Linux

        video_source = 0  # Webcam (varsayılan)
        print("Webcam kullanılıyor...")
        EYE_AR_THRESH = 0.22  # Webcam için normal threshold

    cap = cv2.VideoCapture(video_source)

    # Video özellikleri kontrol
    if isinstance(video_source, str):  # Video dosyası
        fps_video = cap.get(cv2.CAP_PROP_FPS)
        width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        print(f"Video Özellikleri: {width}x{height} @ {fps_video:.1f} FPS")

        # Sınıf videosu tespiti (geniş açı, çok yüz)
        if width > 1280 or height > 720:
            print("HD/4K sınıf videosu tespit edildi - Özel ayarlar aktif")
            EYE_AR_THRESH = 0.16  # Daha da düşük threshold
            face_tracker.max_distance = 150  # Daha toleranslı tracking[1]
        print(f"Video dosyası yükleniyor: {video_source}")
        # Video dosyaları için özel ayarlar
        EYE_AR_THRESH = 0.20  # Video için daha düşük threshold
    else:
        video_source = 0
        print("Webcam kullanılıyor...")
        EYE_AR_THRESH = 0.25  # Webcam için normal threshold

    cap = cv2.VideoCapture(video_source)

    # FPS hesaplama için
    fps_counter = 0
    fps_start_time = cv2.getTickCount()
    fps = 0
    inference_time = 0  # Model inference süresi

    # Emotion analiz için frame skip
    emotion_skip = 5  # Her 5 frame'de bir emotion analizi
    embedding_skip = 60  # Her 60 frame'de bir embedding (daha seyrek)
    frame_count = 0

    # Face embedding kullan/kullanma
    use_face_embedding = False  # Performans için kapalı başlat

    # Göz takibi için parametreler
    EYE_AR_THRESH = 0.18  # Sınıf ortamı için daha düşük threshold
    EYE_AR_CONSEC_FRAMES = 2  # Daha hassas göz kırpma tespiti
    show_eye_landmarks = False  # Göz landmark'larını göster/gizle
    debug_mode = False  # Debug bilgilerini göster/gizle
    use_alternative_eye_detection = True  # Alternatif göz tespiti

    print("Face Tracking başlatıldı. Çıkmak için 'q' tuşuna basın.")
    print("'r' - Tracker reset | 's' - İstatistikleri göster | 'e' - Embedding aç/kapa")
    print("'l' - Göz landmark'larını göster/gizle | 'd' - Debug modu")
    print("'a' - Alternatif göz tespiti aç/kapa")
    print("'+/-' - EAR threshold ayarla (şu an: %.2f)" % EYE_AR_THRESH)

    while cap.isOpened():
        success, frame = cap.read()
        if not success:
            print("Frame okunamadı!")
            break

        frame_count += 1

        # YOLO yüz tespiti
        start_time = cv2.getTickCount()
        results = yolo_model(frame, device=device)
        inference_time = (cv2.getTickCount() - start_time) / cv2.getTickFrequency() * 1000

        boxes = results[0].boxes

        # MediaPipe için RGB dönüşümü
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_results = face_mesh.process(frame_rgb)

        mp_landmarks_list = []
        if mp_results.multi_face_landmarks:
            for face_landmarks in mp_results.multi_face_landmarks:
                mp_landmarks_list.append(landmarks_to_np(face_landmarks, frame.shape[1], frame.shape[0]))

        # Tespit edilen yüzleri hazırla
        detections = []
        if boxes is not None:
            for idx, box in enumerate(boxes):
                x1, y1, x2, y2 = map(int, box.xyxy[0])

                # Çok küçük yüzleri atla - sınıf ortamı için ayarlı
                min_face_size = 30 if isinstance(video_source, str) else 40  # Video dosyası için daha küçük kabul et
                if (x2 - x1) < min_face_size or (y2 - y1) < min_face_size:
                    continue

                detection = {
                    'bbox': (x1, y1, x2, y2),
                    'confidence': float(box.conf[0]) if hasattr(box, 'conf') else 1.0
                }

                face_crop = frame[y1:y2, x1:x2]

                # Emotion analizi (frame skip ile)
                if frame_count % emotion_skip == 0:
                    emotion, emotion_scores = predict_emotion(face_crop)
                    detection['emotion'] = emotion
                    detection['emotion_scores'] = emotion_scores

                # Face embedding (opsiyonel ve daha seyrek)
                if use_face_embedding and frame_count % embedding_skip == 0:
                    embedding = extract_face_embedding(face_crop)
                    if embedding is not None:
                        detection['embedding'] = embedding

                # Kafa pozisyonu ve göz durumu
                if idx < len(mp_landmarks_list):
                    landmarks_2d = mp_landmarks_list[idx]
                    yaw, pitch, roll = get_head_pose(landmarks_2d, frame.shape)
                    detection['yaw'] = yaw

                    # Göz durumu tespiti - İYİLEŞTİRİLMİŞ
                    try:
                        # Küçük yüzler için enhancement
                        enhanced_face = face_crop
                        if (x2 - x1) < 100:  # Küçük yüz
                            enhanced_face = enhance_face_for_eye_detection(face_crop)

                        left_eye, right_eye = get_eye_landmarks(landmarks_2d)

                        if left_eye is None or right_eye is None:
                            # MediaPipe başarısız - alternatif yöntem kullan
                            if use_alternative_eye_detection:
                                eye_state, confidence = detect_eye_state_alternative(enhanced_face)
                                detection['eye_state'] = eye_state
                                detection['ear'] = 0.2 if eye_state == "closed" else 0.3
                                detection['method'] = 'alternative'
                            else:
                                detection['eye_state'] = 'open'
                                detection['ear'] = 0.3
                                detection['method'] = 'default'
                        else:
                            left_ear = calculate_ear(left_eye)
                            right_ear = calculate_ear(right_eye)

                            # Ortalama EAR
                            ear = (left_ear + right_ear) / 2.0
                            detection['ear'] = ear

                            # Adaptif threshold
                            face_width = x2 - x1
                            adaptive_threshold = calculate_adaptive_ear_threshold(face_width, EYE_AR_THRESH)

                            # Hibrit karar verme
                            if face_width < 100 and use_alternative_eye_detection:  # Uzak yüzler için
                                # EAR + alternatif yöntem kombinasyonu
                                alt_state, alt_conf = detect_eye_state_alternative(enhanced_face, ear)

                                if ear < adaptive_threshold and alt_state in ["closed", "possibly_closed"]:
                                    detection['eye_state'] = 'closed'
                                elif ear >= adaptive_threshold and alt_state == "open":
                                    detection['eye_state'] = 'open'
                                else:
                                    # Kararsız durum - alternatif yönteme güven
                                    detection['eye_state'] = 'closed' if alt_state == "closed" else 'open'

                                detection['method'] = 'hybrid'
                            else:
                                # Yakın yüzler için sadece EAR
                                if ear < adaptive_threshold:
                                    detection['eye_state'] = 'closed'
                                else:
                                    detection['eye_state'] = 'open'
                                detection['method'] = 'ear_only'

                            # Debug bilgileri
                            detection['face_width'] = face_width
                            detection['threshold_used'] = adaptive_threshold
                            detection['left_ear'] = left_ear
                            detection['right_ear'] = right_ear
                    except Exception as e:
                        # Fallback
                        detection['eye_state'] = 'open'
                        detection['ear'] = 0.3
                        detection['method'] = 'failed'

                # Opsiyonel: Göz landmark'larını çiz
                if show_eye_landmarks and idx < len(mp_landmarks_list):
                    try:
                        left_eye, right_eye = get_eye_landmarks(landmarks_2d)
                        # Sol göz
                        for point in left_eye:
                            cv2.circle(frame, point, 2, (255, 0, 0), -1)
                        # Sağ göz
                        for point in right_eye:
                            cv2.circle(frame, point, 2, (0, 255, 0), -1)
                    except:
                        pass

                detections.append(detection)

        # Face tracker'ı güncelle
        tracked_faces = face_tracker.update(detections)

        # Sonuçları çiz
        for face_id, face_data in tracked_faces.items():
            x1, y1, x2, y2 = face_data['bbox']

            # EmotionTracker'dan stable emotion al
            stable_emotion = "unknown"
            emotion_confidence = 0

            if face_id in face_tracker.emotion_trackers:
                stable_emotion, emotion_confidence = face_tracker.emotion_trackers[face_id].get_stable_emotion()
            else:
                # Fallback: eski yöntem
                emotion_history = list(face_data['emotion_history'])
                if emotion_history:
                    emotion_counter = Counter(emotion_history)
                    stable_emotion = emotion_counter.most_common(1)[0][0]
                    emotion_confidence = emotion_counter[stable_emotion] / len(emotion_history)

            # Renk seçimi (confidence'a göre)
            if face_data['confidence'] > 0.8:
                color = (0, 255, 0)  # Yeşil - güvenilir
            elif face_data['confidence'] > 0.5:
                color = (0, 255, 255)  # Sarı - orta
            else:
                color = (0, 0, 255)  # Kırmızı - düşük güvenilirlik

            # Çizimler
            cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2)

            # ID ve emotion
            text = f"ID:{face_id} {stable_emotion}"
            if emotion_confidence > 0:
                text += f" ({emotion_confidence:.0%})"
            cv2.putText(frame, text, (x1, y1 - 30),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)

            # Kafa dönüş sayısı ve göz durumu
            cv2.putText(frame, f"Turns: {face_data['head_turn_count']} | Blinks: {face_data.get('blink_count', 0)}",
                        (x1, y2 + 20), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)

            # Göz durumu göstergesi
            eye_state = face_data.get('last_eye_state', 'open')
            eye_closed_ratio = face_data.get('eye_closed_count', 0) / max(face_data['frame_count'], 1)

            # Göz durumu visualizasyonu
            if eye_state == 'closed':
                # Kırmızı dikdörtgen ve uyarı
                cv2.rectangle(frame, (x1 - 5, y1 - 5), (x2 + 5, y2 + 5), (0, 0, 255), 3)
                cv2.putText(frame, "EYES CLOSED!", (x1, y1 - 50),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)

                # Göz bölgesini vurgula (opsiyonel)
                if debug_mode:
                    eye_region_y1 = y1 + int((y2 - y1) * 0.2)
                    eye_region_y2 = y1 + int((y2 - y1) * 0.45)
                    cv2.rectangle(frame, (x1, eye_region_y1), (x2, eye_region_y2), (0, 0, 255), 2)

            # Uzun süreli göz kapalı uyarısı
            if eye_closed_ratio > 0.3:  # %30'dan fazla göz kapalı
                cv2.putText(frame, f"!!! Sleepy ({eye_closed_ratio:.0%}) !!!",
                            (x1, y1 - 70), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)

            # Frame sayısı, stabilite ve EAR
            info_text = f"Frames: {face_data['frame_count']} Stable: {face_data['stable_frames']}"
            if face_id in face_tracker.emotion_trackers and face_tracker.emotion_trackers[face_id].eye_aspect_ratios:
                avg_ear = np.mean(list(face_tracker.emotion_trackers[face_id].eye_aspect_ratios))
                info_text += f" EAR: {avg_ear:.2f}"
            cv2.putText(frame, info_text, (x1, y2 + 40), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 1)

            # Debug bilgileri
            if debug_mode:
                # En son detection'ı bul
                for detection in detections:
                    det_bbox = detection.get('bbox')
                    if det_bbox and abs(det_bbox[0] - x1) < 10:  # Bu yüze ait detection
                        if 'ear' in detection:
                            debug_text = f"L:{detection.get('left_ear', 0):.2f} R:{detection.get('right_ear', 0):.2f}"
                            debug_text += f" T:{detection.get('threshold_used', 0.25):.2f}"
                            debug_text += f" [{detection.get('method', 'unknown')}]"
                            cv2.putText(frame, debug_text, (x1, y2 + 80),
                                        cv2.FONT_HERSHEY_SIMPLEX, 0.4, (255, 255, 0), 1)
                        break

            # Emotion değişim sayısı (varsa)
            if face_id in face_tracker.emotion_trackers:
                changes = face_tracker.emotion_trackers[face_id].emotion_change_count
                cv2.putText(frame, f"Emotion changes: {changes}",
                            (x1, y2 + 60), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 1)

        # FPS hesapla ve göster
        fps_counter += 1
        if fps_counter % 30 == 0:
            fps_end_time = cv2.getTickCount()
            fps = 30 / ((fps_end_time - fps_start_time) / cv2.getTickFrequency())
            fps_start_time = fps_end_time

        # Üst bilgi paneli
        cv2.rectangle(frame, (0, 0), (frame.shape[1], 60), (0, 0, 0), -1)
        status_text = f"FPS: {fps:.1f} | Faces: {len(tracked_faces)} | Frame: {frame_count}"
        if use_face_embedding:
            status_text += " | Embedding: ON"
        cv2.putText(frame, status_text,
                    (10, 20), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)

        # İkinci satır - threshold ve debug bilgisi
        info_text = f"EAR Threshold: {EYE_AR_THRESH:.2f} | Model: {model_size} | Inference: {inference_time:.1f}ms"
        if debug_mode:
            info_text += " | DEBUG: ON"
        cv2.putText(frame, info_text,
                    (10, 45), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (200, 200, 200), 1)

        cv2.imshow("Advanced Face Tracking", frame)

        key = cv2.waitKey(1) & 0xFF
        if key == ord("q"):
            break
        elif key == ord("r"):  # Reset
            face_tracker = FaceTracker(max_disappeared=30, max_distance=120)
            print("Tracker resetlendi!")
        elif key == ord("e"):  # Embedding toggle
            use_face_embedding = not use_face_embedding
            print(f"Face embedding: {'AÇIK' if use_face_embedding else 'KAPALI'}")
        elif key == ord("l"):  # Landmark toggle
            show_eye_landmarks = not show_eye_landmarks
            print(f"Göz landmarks: {'AÇIK' if show_eye_landmarks else 'KAPALI'}")
        elif key == ord("d"):  # Debug toggle
            debug_mode = not debug_mode
            print(f"Debug modu: {'AÇIK' if debug_mode else 'KAPALI'}")
        elif key == ord("a"):  # Alternative eye detection toggle
            use_alternative_eye_detection = not use_alternative_eye_detection
            print(f"Alternatif göz tespiti: {'AÇIK' if use_alternative_eye_detection else 'KAPALI'}")
        elif key == ord("+") or key == ord("="):  # Threshold artır
            EYE_AR_THRESH += 0.01
            print(f"EAR Threshold: {EYE_AR_THRESH:.2f}")
        elif key == ord("-") or key == ord("_"):  # Threshold azalt
            EYE_AR_THRESH -= 0.01
            print(f"EAR Threshold: {EYE_AR_THRESH:.2f}")
        elif key == ord("s"):  # İstatistikleri göster
            print("\n=== ANLIK İSTATİSTİKLER ===")
            for face_id, face_data in tracked_faces.items():
                if face_id in face_tracker.emotion_trackers:
                    stable_emotion, confidence = face_tracker.emotion_trackers[face_id].get_stable_emotion()
                    eye_state = face_data.get('last_eye_state', 'open')
                    eye_closed_ratio = face_data.get('eye_closed_count', 0) / max(face_data['frame_count'], 1)

                    print(f"Face {face_id}: {stable_emotion} ({confidence:.0%}), "
                          f"Turns: {face_data['head_turn_count']}, "
                          f"Blinks: {face_data.get('blink_count', 0)}, "
                          f"Eyes: {eye_state} (Closed {eye_closed_ratio:.0%}), "
                          f"Changes: {face_tracker.emotion_trackers[face_id].emotion_change_count}")

    # Temizlik
    cap.release()
    cv2.destroyAllWindows()

    # Sonuçları yazdır
    print("\n=== FINAL RESULTS ===")
    print(f"Toplam takip edilen yüz sayısı: {face_tracker.next_id}")
    print(f"Aktif yüz sayısı: {len(face_tracker.faces)}")
    print("\nDetaylı Bilgiler:")

    for face_id, face_data in face_tracker.faces.items():
        print(f"\nFace ID {face_id}:")

        # Emotion tracker'dan bilgi al
        if face_id in face_tracker.emotion_trackers:
            stable_emotion, confidence = face_tracker.emotion_trackers[face_id].get_stable_emotion()
            emotion_tracker = face_tracker.emotion_trackers[face_id]

            print(f"  - Stable Emotion: {stable_emotion} ({confidence:.0%})")
            print(f"  - Emotion Changes: {emotion_tracker.emotion_change_count}")

            # Emotion dağılımı
            emotion_dist = {}
            for emotion, scores in emotion_tracker.emotion_scores.items():
                if scores:
                    emotion_dist[emotion] = np.mean(list(scores))

            if emotion_dist:
                sorted_emotions = sorted(emotion_dist.items(), key=lambda x: x[1], reverse=True)
                print(f"  - Emotion Distribution:")
                for emotion, score in sorted_emotions[:3]:  # Top 3
                    print(f"    • {emotion}: {score:.1f}%")

        print(f"  - Head Turns: {face_data['head_turn_count']}")
        print(f"  - Blink Count: {face_data.get('blink_count', 0)}")
        print(f"  - Eye Closed Frames: {face_data.get('eye_closed_count', 0)}")
        print(f"  - Eye Closed Ratio: {face_data.get('eye_closed_count', 0) / max(face_data['frame_count'], 1):.1%}")
        print(f"  - Total Frames: {face_data['frame_count']}")
        print(f"  - Stable Frames: {face_data['stable_frames']}")
        print(f"  - Tracking Confidence: {face_data['confidence']:.2f}")

        # Dikkat skoru hesapla
        attention_score = 100
        attention_score -= face_data['head_turn_count'] * 5
        attention_score -= face_data.get('blink_count', 0) * 2  # Her göz kırpma -2
        attention_score -= (face_data.get('eye_closed_count', 0) / max(face_data['frame_count'],
                                                                       1)) * 100  # Göz kapalı oranı

        if face_id in face_tracker.emotion_trackers:
            attention_score -= face_tracker.emotion_trackers[face_id].emotion_change_count * 3
            # Negatif duygular için ekstra penalty
            stable_emotion, _ = face_tracker.emotion_trackers[face_id].get_stable_emotion()
            if stable_emotion in ['sad', 'angry', 'fear']:
                attention_score -= 15

        attention_score = max(0, attention_score)
        print(f"  - Estimated Attention Score: {attention_score}/100")

        # Uyku/dikkat durumu
        eye_closed_ratio = face_data.get('eye_closed_count', 0) / max(face_data['frame_count'], 1)
        if eye_closed_ratio > 0.5:
            print(f"  - ⚠️  UYARI: Öğrenci uyuyor olabilir!")
        elif eye_closed_ratio > 0.3:
            print(f"  - ⚠️  UYARI: Öğrenci uykulu görünüyor!")
        elif face_data.get('blink_count', 0) / max(face_data['frame_count'], 1) > 0.5:
            print(f"  - ⚠️  UYARI: Aşırı göz kırpma - yorgunluk belirtisi!")