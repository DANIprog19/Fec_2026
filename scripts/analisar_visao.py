import sys
import os
import re
import ollama

def normalizar_nome(nome):
    """
    Converte nomes comuns em inglês para português do Brasil.
    Também limpa espaços e caracteres desnecessários.
    """

    if not nome:
        return ""

    nome = nome.strip().lower()

    nome = re.sub(r"\s+", " ", nome)

    mapa = {
        "mug": "caneca",
        "cup": "xícara",
        "coffee cup": "xícara",
        "tea cup": "xícara",

        "scissors": "tesoura",
        "scissor": "tesoura",

        "screwdriver": "chave de fenda",
        "flathead screwdriver": "chave de fenda",

        "hammer": "martelo",

        "pliers": "alicate",
        "plier": "alicate",

        "pen": "caneta",
        "pencil": "lápis",

        "bottle": "garrafa",

        "glass": "copo",
        "cup glass": "copo",

        "spoon": "colher",
        "fork": "garfo",
        "knife": "faca",

        "phone": "celular",
        "smartphone": "celular",
        "mobile phone": "celular",

        "keyboard": "teclado",
        "mouse": "mouse",
        "computer mouse": "mouse",

        "calculator": "calculadora",
        "watch": "relógio",

        "screw": "parafuso",
        "nut": "porca",
        "bolt": "parafuso",

        "ruler": "régua",

        "book": "livro",
        "notebook": "caderno",
        "backpack": "mochila",
        "bag": "bolsa",
        "chair": "cadeira",
        "table": "mesa",
        "lamp": "lâmpada",
        "bowl": "tigela",
        "plate": "prato",
        "remote control": "controle remoto",
        "remote": "controle remoto",
        "headphones": "fone de ouvido",
        "earphones": "fone de ouvido",
        "glasses": "óculos",
        "keys": "chaves",
        "key": "chave",
    }

    return mapa.get(nome, nome)

def extrair_nome(resposta):
    """
    Extrai somente o nome do objeto.
    """

    if not resposta:
        return ""

    resposta = resposta.strip()

    if "|" in resposta:
        nome = resposta.split("|", 1)[0].strip()
    else:
        nome = resposta

    nome = nome.replace("```", "").strip()

    nome = re.sub(
        r"^(o objeto é|o objeto parece ser|"
        r"objeto:|nome:|identificação:|"
        r"objeto identificado:)\s*",
        "",
        nome,
        flags=re.IGNORECASE
    )

    nome = nome.strip("\"'`")

    return normalizar_nome(nome)

def analisar_imagem(caminho_imagem, dica_usuario=""):


    if not caminho_imagem:
        print(
            "objeto não identificado | "
            "Nenhum caminho de imagem foi fornecido."
        )
        return

    if not os.path.isfile(caminho_imagem):
        print(
            "objeto não identificado | "
            "Arquivo de imagem não encontrado."
        )
        return

    dica_usuario = (
        dica_usuario.strip()
        if dica_usuario
        else ""
    )

    prompt = f"""
Você é um sistema de visão computacional especializado
em identificar objetos físicos.

ANALISE A IMAGEM COM MUITA ATENÇÃO.

Existe uma mão segurando um objeto.

Sua tarefa é identificar EXATAMENTE o objeto que está
sendo segurado pela mão.

============================================================
REGRA MAIS IMPORTANTE
============================================================

IGNORE completamente:

- a mão
- braços
- pessoas
- rosto
- mesa
- fundo
- parede
- objetos atrás
- objetos próximos
- sombras
- reflexos
- elementos da câmera

CONCENTRE-SE SOMENTE NO OBJETO PRINCIPAL
QUE ESTÁ SENDO SEGURADO.

============================================================
IDENTIFICAÇÃO VISUAL
============================================================

Observe cuidadosamente:

- formato
- silhueta
- tamanho
- proporções
- material aparente
- partes visíveis
- alça
- cabo
- abertura
- ponta
- lâmina
- botões
- encaixes
- textura
- características físicas

Não escolha um objeto apenas porque ele parece provável.

Escolha o objeto que melhor corresponde
à aparência física observada na imagem.

============================================================
DICA DO USUÁRIO
============================================================

A dica fornecida pelo usuário é:

{dica_usuario if dica_usuario else "Nenhuma dica fornecida."}

A dica pode ajudar na identificação,
MAS NÃO DEVE SUBSTITUIR A ANÁLISE VISUAL.

Se a dica for "xícara", por exemplo,
procure visualmente uma xícara.

Se a imagem mostrar claramente outro objeto,
priorize aquilo que realmente aparece na imagem.

============================================================
IDIOMA
============================================================

O NOME DO OBJETO DEVE SER SEMPRE EM
PORTUGUÊS DO BRASIL.

NUNCA responda o nome em inglês.

Exemplos:

mug → caneca
cup → xícara
scissors → tesoura
screwdriver → chave de fenda
hammer → martelo
pliers → alicate
pen → caneta
pencil → lápis
bottle → garrafa
phone → celular

============================================================
RESPOSTA
============================================================

Responda EXATAMENTE neste formato:

NOME_EM_PORTUGUES | EXPLICAÇÃO

Não coloque nenhuma outra coisa antes ou depois.

A explicação deve ter aproximadamente 3 frases.

Explique:

- o que é o objeto;
- para que serve;
- uma característica visual ou funcional interessante.

Não invente informações históricas.

============================================================
EXEMPLO
============================================================

xícara | Uma xícara é um recipiente utilizado principalmente para servir bebidas como café e chá. Normalmente possui uma abertura superior e uma alça lateral para facilitar o manuseio. Seu formato permite o consumo confortável de bebidas.

============================================================
AGORA ANALISE A IMAGEM.
"""



    try:


        print(
            "Analisando imagem...",
            file=sys.stderr,
            flush=True
        )

        response = ollama.chat(
            model="qwen3-vl:2b",
            messages=[
                {
                    "role": "user",
                    "content": prompt,
                    "images": [caminho_imagem]
                }
            ]
        )


        conteudo = (
            response
            .get("message", {})
            .get("content", "")
        )

        conteudo = conteudo.strip()

        conteudo = conteudo.replace("```", "").strip()

        if not conteudo:

            print(
                "objeto não identificado | "
                "O modelo de visão não retornou uma resposta."
            )

            return


        if "|" in conteudo:

            nome_bruto, explicacao = conteudo.split(
                "|",
                1
            )

            nome = extrair_nome(nome_bruto)

            explicacao = explicacao.strip()

        else:

            nome = extrair_nome(conteudo)

            explicacao = (
                "Objeto identificado visualmente pelo "
                "sistema de visão computacional."
            )


        nome = normalizar_nome(nome)


        if not nome:

            if dica_usuario:

                nome = normalizar_nome(
                    dica_usuario
                )

            else:

                nome = "objeto não identificado"


        if not explicacao:

            explicacao = (
                "O objeto foi identificado pela análise "
                "visual realizada pelo sistema."
            )


        explicacao = explicacao.replace(
            "\n",
            " "
        )

        explicacao = re.sub(
            r"\s+",
            " ",
            explicacao
        ).strip()

        print(
            f"{nome} | {explicacao}",
            flush=True
        )

    except Exception as e:

        print(
            f"Erro de visão: {e}",
            file=sys.stderr,
            flush=True
        )

        print(
            "objeto não identificado | "
            "Não foi possível analisar a imagem.",
            flush=True
        )

if __name__ == "__main__":

    if len(sys.argv) >= 2:

        caminho = sys.argv[1]

        dica = (
            sys.argv[2]
            if len(sys.argv) >= 3
            else ""
        )

        analisar_imagem(
            caminho,
            dica
        )

    else:

        print(
            "objeto não identificado | "
            "Nenhuma imagem foi fornecida.",
            flush=True
        )