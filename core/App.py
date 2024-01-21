from PyQt6 import QtWidgets

from gui.FormMain import FormMain

class App(QtWidgets.QApplication):
  main_window = None
  
  def __init__(self, args):
    super(App, self).__init__(args)
    self.main_window = FormMain()
    self.main_window.show()