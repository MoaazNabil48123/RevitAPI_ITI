using Autodesk.Revit.DB;
using finalProject.RevitMethods;
using finalProject.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using finalProject.TabCreator.ButtonClasses;
using finalProject.Command;

namespace finalProject.ViewModel
{
    internal class ColumnCreation_VM : ViewModelBase
    {
        #region Fields
        private double _topOffset = 0;
        private double _bottomOffset = 0;
        private string _SelectedTopLevel;
        private string _selectedBottomLevel;

        #endregion

        #region Property

        public List<string> Levels { get; set; }
        public double TopOffset
        {
            get { return _topOffset; }
            set
            {
                _topOffset = value;
                OnPropertyChanged();
            }
        }
        public double BottomOffset
        {
            get { return _bottomOffset; }
            set
            {
                _bottomOffset = value;
                OnPropertyChanged();
            }
        }
        public string SelectedTopLevel
        {
            get { return _SelectedTopLevel; }
            set
            {
                _SelectedTopLevel = value;
                OnPropertyChanged();
            }
        }
        public string SelectedBottomLevel
        {
            get { return _selectedBottomLevel; }
            set
            {
                _selectedBottomLevel = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public MyCommand OkCommand { get; set; }
        #endregion

        #region Methods
        public void Ok(object parameter)
        {
            (parameter as ColumnCreation).Close();
            RevitMethods.RevitMethods.CreateColumnsFromCad(ColumnCreationButton.Uidoc, SelectedTopLevel, SelectedBottomLevel, TopOffset, BottomOffset);
        }
        #endregion

        #region Constructor
        public ColumnCreation_VM()
        {
            #region Level Names
            UIDocument uIDocument = ColumnCreationButton.Uidoc;
            Document doc = uIDocument.Document;
            Levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Select(l => l.Name).ToList();
            #endregion

            OkCommand = new MyCommand(Ok);
        }
        #endregion
    }
}
