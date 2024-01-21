import sys

from core.App import App

def main():
    app = App(sys.argv)
    app.exec()

if __name__ == "__main__":
    main()