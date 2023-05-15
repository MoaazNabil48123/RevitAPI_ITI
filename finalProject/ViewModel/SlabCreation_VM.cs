using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using finalProject.Command;
using finalProject.TabCreator.ButtonClasses;
using finalProject.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace finalProject.ViewModel
{
    internal class SlabCreation_VM : ViewModelBase
    {
        #region Fields
        private double _offset = 0;
        private string _SelectedLevel;
        private string _selectedType;

        #endregion

        #region Property

        public List<string> Levels { get; set; }
        public List<string> Types { get; set; }
        public double Offset
        {
            get { return _offset; }
            set
            {
                _offset = value;
                OnPropertyChanged();
            }
        }
        public string SelectedLevel
        {
            get { return _SelectedLevel; }
            set
            {
                _SelectedLevel = value;
                OnPropertyChanged();
            }
        }
        public string SelectedType
        {
            get { return _selectedType; }
            set
            {
                _selectedType = value;
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
            (parameter as SlabCreation).Close();
            RevitMethods.RevitMethods.CreateFloorFromCad(SlabCreationButton.Uidoc, SelectedType, SelectedLevel, Offset);
        }
        #endregion

        #region Constructor
        public SlabCreation_VM()
        {
            #region Level Names
            UIDocument uIDocument = SlabCreationButton.Uidoc;
            Document doc = uIDocument.Document;
            Levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Select(l => l.Name).ToList();
            Types = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).Select(l => l.Name).ToList();
            #endregion

            OkCommand = new MyCommand(Ok);
        }
        #endregion
    }
}
