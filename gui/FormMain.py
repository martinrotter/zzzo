from PyQt6 import QtWidgets

from gui.ui.Ui_FormMain import Ui_FormMain

class FormMain(QtWidgets.QMainWindow, Ui_FormMain):
    def __init__(self, *args, obj=None, **kwargs):
        super(FormMain, self).__init__(*args, **kwargs)
        self.setupUi(self)
        
        self.m_actionQuit.triggered.connect(self.quitForm)
        
    def quitForm(self):
      QtWidgets.QMessageBox.information(self, "tit", "txt")
      self.close()
      