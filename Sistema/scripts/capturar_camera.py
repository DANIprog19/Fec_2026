import cv2
import argparse
import sys

parser = argparse.ArgumentParser()
parser.add_argument('--out', required=True, help='Caminho de saída da imagem')
args = parser.parse_args()

cap = cv2.VideoCapture(0, cv2.CAP_V4L2)

if not cap.isOpened():
    cap = cv2.VideoCapture(1, cv2.CAP_V4L2)

if not cap.isOpened():
    print("Erro: Não foi possível acessar a webcam.", file=sys.stderr)
    sys.exit(1)

for _ in range(5):
    cap.read()

ret, frame = cap.read()
cap.release()

if ret and frame is not None:
    cv2.imwrite(args.out, frame)
    print("Foto capturada com sucesso!")
else:
    print("Erro: Falha ao capturar imagem da câmera.", file=sys.stderr)
    sys.exit(1)