import os
import signal
import sys

# === FIX DLA WINDOWS (BRAK SIGHUP) ===
# Ten kod naprawia błąd "AttributeError: module 'signal' has no attribute 'SIGHUP'"
if sys.platform == "win32" and not hasattr(signal, "SIGHUP"):
    # Przypisujemy sztuczną wartość, żeby biblioteka nie zgłaszała błędu
    signal.SIGHUP = 1

from crewai import Agent, Task, Crew, Process
from langchain_google_genai import ChatGoogleGenerativeAI
# Importujemy narzędzie do pisania plików
from crewai_tools import FileWriterTool, FileReadTool

# ==========================================
# 1. KONFIGURACJA ŚRODOWISKA
# ==========================================

# ŚCIEŻKA DO TWOJEGO PROJEKTU UNITY (Folder Scripts)
# Agent będzie tu "mieszkał".
WORK_DIR = "C:\\Unity_Version\\Retentia AI\\Assets\\_Retentia\\Runtime"

# Sprawdzamy, czy folder istnieje, żeby nie było błędu
if not os.path.exists(WORK_DIR):
    os.makedirs(WORK_DIR)

# Konfiguracja Gemini
llm = ChatGoogleGenerativeAI(
    model="gemini-pro",
    google_api_key="AIzaSyCzBCdnbTLxlI2_dbIgHukQhULKcCDnDLY",
    temperature=0.5
)

# ==========================================
# 2. DAWIANIE AGENTOWI "RĄK" (NARZĘDZIA)
# ==========================================

# Narzędzie: Pozwala agentowi tworzyć i zapisywać pliki
# Ważne: Agent sam decyduje o nazwie pliku i treści!
file_writer_tool = FileWriterTool()

# ==========================================
# 3. AGENT - ARCHITEKT SYSTEMÓW
# ==========================================

senior_architect = Agent(
    role='Lead Unity Architect',
    goal='Zaprojektować i wdrażyć (stworzyć pliki) kompletny system przeciwników.',
    backstory=f"""
    Jesteś głównym architektem gry. Nie lubisz spaghetti code.
    Wolisz rozbijać kod na mniejsze, logiczne pliki (SOLID principles).
    Masz uprawnienia do tworzenia plików bezpośrednio w folderze: {WORK_DIR}.
    Kiedy uznasz, że potrzebna jest nowa klasa - po prostu tworzysz dla niej plik .cs.
    """,
    llm=llm,
    tools=[file_writer_tool],  # <--- TU DAJESZ MU NARZĘDZIE
    verbose=True,
    allow_delegation=False
)

# ==========================================
# 4. ZADANIE (OTWARTE)
# ==========================================

task_create_enemy_system = Task(
    description=f"""
    Stwórz kompletny system wrogów do gry Space Shooter w Unity.

    Wymagania:
    1. Stwórz tyle plików .cs, ile uznasz za stosowne (np. oddzielnie logika ruchu, oddzielnie dane wroga).
    2. Kod musi być w C#.
    3. Pliki muszą być zapisane w katalogu: {WORK_DIR}.
    4. WAŻNE: Upewnij się, że nazwy plików odpowiadają nazwom klas wewnątrz (Unity tego wymaga).

    Zacznij od analizy, jakie pliki są potrzebne, a potem je stwórz jeden po drugim używając narzędzia zapisu.
    """,
    agent=senior_architect,
    expected_output="Raport: Lista stworzonych plików i krótki opis, co robią."
)

# ==========================================
# 5. URUCHOMIENIE
# ==========================================

crew = Crew(
    agents=[senior_architect],
    tasks=[task_create_enemy_system],
    process=Process.sequential
)

print("### AGENT ROZPOCZYNA PRACĘ NAD STRUKTURĄ PROJEKTU ###")
result = crew.kickoff()
print(result)