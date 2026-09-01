import os
import sys
import base64
import requests
import time

from PIL import Image, ImageOps
from io import BytesIO


OLLAMA_URL = "http://localhost:11434/api/chat"
OLLAMA_MODEL = "qwen3-vl:2b-instruct"

MAX_IMAGE_SIZE = 640


def carregar_imagem(caminho):

    if not os.path.exists(caminho):

        print(
            f"[QWEN] ❌ Imagem não encontrada: {caminho}"
        )

        return None

    try:

        inicio = time.perf_counter()

        imagem = Image.open(caminho)

        print(
            f"[QWEN] 📷 Original: "
            f"{imagem.width}x{imagem.height}"
        )

        try:
            imagem = ImageOps.exif_transpose(imagem)
        except Exception:
            pass

        if imagem.mode != "RGB":
            imagem = imagem.convert("RGB")

        imagem.thumbnail(
            (
                MAX_IMAGE_SIZE,
                MAX_IMAGE_SIZE
            ),
            Image.Resampling.LANCZOS
        )

        print(
            f"[QWEN] 📐 Enviada: "
            f"{imagem.width}x{imagem.height}"
        )

        buffer = BytesIO()

        imagem.save(
            buffer,
            format="JPEG",
            quality=75,
            optimize=True
        )

        imagem_base64 = base64.b64encode(
            buffer.getvalue()
        ).decode("utf-8")

        tempo = (
            time.perf_counter()
            - inicio
        )

        print(
            f"[QWEN] ⚙️ Preparação: "
            f"{tempo:.2f}s"
        )

        return imagem_base64

    except Exception as ex:

        print(
            f"[QWEN] ❌ Erro ao preparar imagem: "
            f"{ex}"
        )

        return None

def limpar_resposta(texto):

    if not texto:

        return "Objeto não identificado"

    texto = texto.strip()

    texto = texto.replace("\n", " ")

    texto = " ".join(
        texto.split()
    )

    texto = texto.strip(
        "\"'"
    )

    texto = texto.strip(
        ".,:;!?-"
    )

    if not texto:

        return "Objeto não identificado"

    return texto


def analisar_imagem(caminho_imagem):

    inicio_total = time.perf_counter()


    imagem_base64 = carregar_imagem(
        caminho_imagem
    )

    if imagem_base64 is None:

        return "Objeto não identificado"


    prompt = """
Identify the main object in this image.

Return only the object's name in Portuguese.

Use a short and natural name with 1 to 4 words.

Examples:
caneta
copo
celular
controle de videogame
caixa de café
cachorro

Do not explain.
Do not describe the image.
Do not write a sentence.
"""


    payload = {

        "model": OLLAMA_MODEL,

        "messages": [

            {
                "role": "user",

                "content": prompt,

                "images": [
                    imagem_base64
                ]
            }

        ],

        "stream": False,

        "think": False,

        "options": {

            "temperature": 0,

            "num_predict": 12
        }
    }

    try:

        print(
            "[QWEN] 🔎 Analisando..."
        )

        print(
            f"[QWEN] 🤖 Modelo: "
            f"{OLLAMA_MODEL}"
        )

        print(
            "[QWEN] 🧠 Thinking: DESATIVADO"
        )

        inicio_ia = time.perf_counter()

        resposta = requests.post(

            OLLAMA_URL,

            json=payload,

            timeout=180
        )

        tempo_ia = (
            time.perf_counter()
            - inicio_ia
        )

        print(
            f"[QWEN] ⏱️ Tempo IA: "
            f"{tempo_ia:.2f}s"
        )

        if resposta.status_code != 200:

            print(
                f"[QWEN] ❌ HTTP "
                f"{resposta.status_code}"
            )

            print(
                resposta.text
            )

            return "Objeto não identificado"

        dados = resposta.json()

        message = dados.get(
            "message",
            {}
        )

        texto = message.get(
            "content",
            ""
        )

        texto = limpar_resposta(
            texto
        )


        print(
            f"[QWEN] 📄 Resposta: "
            f"{texto}"
        )

        if (
            not texto
            or texto == "Objeto não identificado"
        ):

            print(
                "[QWEN] ❌ Resposta vazia."
            )

            return "Objeto não identificado"


        tempo_total = (
            time.perf_counter()
            - inicio_total
        )

        print(
            f"[QWEN] ✅ Objeto final: "
            f"{texto}"
        )

        print(
            f"[QWEN] ⏱️ Tempo total: "
            f"{tempo_total:.2f}s"
        )

        return texto


    except requests.exceptions.Timeout:

        print(
            "[QWEN] ❌ Timeout."
        )

        return "Objeto não identificado"


    except requests.exceptions.ConnectionError:

        print(
            "[QWEN] ❌ Não foi possível conectar ao Ollama."
        )

        return "Objeto não identificado"


    except requests.exceptions.RequestException as ex:

        print(
            f"[QWEN] ❌ Erro HTTP: "
            f"{ex}"
        )

        return "Objeto não identificado"


    except Exception as ex:

        print(
            f"[QWEN] ❌ Erro inesperado: "
            f"{ex}"
        )

        return "Objeto não identificado"

if __name__ == "__main__":

    if len(sys.argv) < 2:

        print(
            "Uso:"
        )

        print(
            "python3 scripts/ConsultarListaImagens.py "
            "caminho_da_imagem"
        )

        sys.exit(1)


    caminho = sys.argv[1]


    resultado = analisar_imagem(
        caminho
    )


    print()

    print(
        "======================================"
    )

    print(
        f"OBJETO_FINAL={resultado}"
    )

    print(
        "======================================"
    )