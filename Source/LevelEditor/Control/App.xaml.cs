﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Nick Pruehs">
//   Copyright 2013 Nick Pruehs.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace LevelEditor.Control
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using System.Xml;

    using LevelEditor.Model;
    using LevelEditor.View;

    using Microsoft.Win32;

    /// <summary>
    /// Main application controller.
    /// </summary>
    public partial class App
    {
        #region Constants

        private const string MapFileExtension = ".map";

        private const string MapFileFilter = "Map files (.map)|*.map";

        private const string XmlElementHeight = "Height";

        private const string XmlElementPositionX = "X";

        private const string XmlElementPositionY = "Y";

        private const string XmlElementTiles = "Tiles";

        private const string XmlElementType = "Type";

        private const string XmlElementWidth = "Width";

        #endregion

        #region Fields

        /// <summary>
        /// Window showing information about the application.
        /// </summary>
        private AboutWindow aboutWindow;

        /// <summary>
        /// Current active brush.
        /// </summary>
        private MapTileType currentBrush;

        /// <summary>
        /// Main application window.
        /// </summary>
        private MainWindow mainWindow;

        /// <summary>
        /// Current map being edited by the user.
        /// </summary>
        private Map map;

        /// <summary>
        /// Window allowing to specify the properties of a new map to create.
        /// </summary>
        private NewMapWindow newMapWindow;

        /// <summary>
        /// Available map tile images.
        /// </summary>
        private Dictionary<string, BitmapImage> tileImages;

        /// <summary>
        /// Available map tile types.
        /// </summary>
        private Dictionary<string, MapTileType> tileTypes;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Whether the application can be shut down.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if the application can be shut down, and <c>false</c> otherwise.
        /// </returns>
        public bool CanExecuteClose()
        {
            return true;
        }

        /// <summary>
        /// Whether the About window of the application can be shown.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if the About window of the application can be shown, and <c>false</c> otherwise.
        /// </returns>
        public bool CanExecuteHelp()
        {
            return true;
        }

        /// <summary>
        /// Whether the window that allows specifying the properties of a new map to create can be shown.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if the New Map window can be shown, and <c>false</c> otherwise.
        /// </returns>
        public bool CanExecuteNew()
        {
            return true;
        }

        /// <summary>
        /// Whether an existing map can be opened.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if an existing map can be opened, and <c>false</c> otherwise.
        /// </returns>
        public bool CanExecuteOpen()
        {
            return true;
        }

        /// <summary>
        /// Whether the current map can be saved.
        /// </summary>
        /// <returns>
        /// <c>true</c>, the current map can be saved, and <c>false</c> otherwise.
        /// </returns>
        public bool CanExecuteSaveAs()
        {
            return this.map != null;
        }

        /// <summary>
        /// Creates a new map based on the properties of the New Map window.
        /// </summary>
        public void CreateMap()
        {
            // Parse map dimensions.
            int width;
            int height;

            try
            {
                width = int.Parse(this.newMapWindow.TextBoxMapWidth.Text);
            }
            catch (FormatException)
            {
                this.ShowErrorMessage("Incorrect Width", "Please specify a map width.");
                return;
            }

            try
            {
                height = int.Parse(this.newMapWindow.TextBoxMapHeight.Text);
            }
            catch (FormatException)
            {
                this.ShowErrorMessage("Incorrect Height", "Please specify a map height.");
                return;
            }

            // Create new map.
            try
            {
                this.map = new Map(width, height);
            }
            catch (ArgumentOutOfRangeException e)
            {
                this.ShowErrorMessage("Incorrect Map Size", e.Message);
                return;
            }

            // Fill with tiles.
            var defaultMapTile = this.newMapWindow.SelectedMapTileType;

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    this.map[x, y] = new MapTile(x, y, defaultMapTile);
                }
            }

            // Show new map tiles.
            this.UpdateMapCanvas();

            // Close window.
            this.newMapWindow.Close();
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        public void ExecuteClose()
        {
            this.Shutdown();
        }

        /// <summary>
        /// Shows the About window of the application.
        /// </summary>
        public void ExecuteHelp()
        {
            if (this.aboutWindow == null || !this.aboutWindow.IsLoaded)
            {
                this.aboutWindow = new AboutWindow();
            }

            this.aboutWindow.Show();
            this.aboutWindow.Focus();
        }

        /// <summary>
        /// Shows the window that allows specifying the properties of a new map to create.
        /// </summary>
        public void ExecuteNew()
        {
            if (this.newMapWindow == null || !this.newMapWindow.IsLoaded)
            {
                this.newMapWindow = new NewMapWindow();
                this.newMapWindow.SetMapTileTypes(this.tileTypes.Keys);
            }

            this.newMapWindow.Show();
            this.newMapWindow.Focus();
        }

        /// <summary>
        /// Shows an open file dialog box and opens the specified map file.
        /// </summary>
        public void ExecuteOpen()
        {
            // Show open file dialog box.
            OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    AddExtension = true, 
                    CheckFileExists = true, 
                    CheckPathExists = true, 
                    DefaultExt = MapFileExtension, 
                    FileName = "Another Map", 
                    Filter = MapFileFilter, 
                    ValidateNames = true
                };

            var result = openFileDialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            // Open map file.
            using (var stream = openFileDialog.OpenFile())
            {
                using (var reader = XmlReader.Create(stream))
                {
                    try
                    {
                        // Read document element.
                        reader.Read();

                        // Read map dimensions.
                        reader.ReadToFollowing(XmlElementWidth);
                        reader.ReadStartElement();
                        var width = reader.ReadContentAsInt();

                        reader.ReadToFollowing(XmlElementHeight);
                        reader.ReadStartElement();
                        var height = reader.ReadContentAsInt();

                        // Create new map.
                        var loadedMap = new Map(width, height);

                        // Read map tiles.
                        reader.ReadToFollowing(XmlElementTiles);

                        for (var i = 0; i < width * height; i++)
                        {
                            reader.ReadToFollowing(XmlElementPositionX);
                            reader.ReadStartElement();
                            var x = reader.ReadContentAsInt();

                            reader.ReadToFollowing(XmlElementPositionY);
                            reader.ReadStartElement();
                            var y = reader.ReadContentAsInt();

                            reader.ReadToFollowing(XmlElementType);
                            reader.ReadStartElement();
                            var type = reader.ReadContentAsString();

                            var mapTile = new MapTile(x, y, type);
                            loadedMap[x, y] = mapTile;
                        }

                        // Show new map tiles.
                        this.map = loadedMap;
                        this.UpdateMapCanvas();
                    }
                    catch (XmlException)
                    {
                        this.ShowErrorMessage("Incorrect map file", "Please specify a valid map file!");
                    }
                    catch (InvalidCastException)
                    {
                        this.ShowErrorMessage("Incorrect map file", "Please specify a valid map file!");
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        this.ShowErrorMessage("Incorrect map file", "Please specify a valid map file!");
                    }
                }
            }
        }

        /// <summary>
        /// Shows a save file dialog box and saves the current map to the specified file.
        /// </summary>
        public void ExecuteSaveAs()
        {
            // Show save file dialog box.
            SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    AddExtension = true, 
                    CheckPathExists = true, 
                    DefaultExt = MapFileExtension, 
                    FileName = "Another Map", 
                    Filter = MapFileFilter, 
                    ValidateNames = true
                };

            var result = saveFileDialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            // Open file stream.
            using (var stream = saveFileDialog.OpenFile())
            {
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };

                using (var writer = XmlWriter.Create(stream, settings))
                {
                    // Write document element.
                    writer.WriteStartElement("Map");

                    // Write the namespace declaration.
                    writer.WriteAttributeString(
                        "xmlns", "map", null, "http://www.npruehs.de/teaching/tool-development/");

                    // Write map dimensions.
                    writer.WriteElementString(XmlElementWidth, this.map.Width.ToString(CultureInfo.InvariantCulture));
                    writer.WriteElementString(XmlElementHeight, this.map.Height.ToString(CultureInfo.InvariantCulture));

                    // Write map tiles.
                    writer.WriteStartElement(XmlElementTiles);
                    foreach (var mapTile in this.map.Tiles)
                    {
                        writer.WriteStartElement("MapTile");
                        writer.WriteElementString(
                            XmlElementPositionX, mapTile.Position.X.ToString(CultureInfo.InvariantCulture));
                        writer.WriteElementString(
                            XmlElementPositionY, mapTile.Position.Y.ToString(CultureInfo.InvariantCulture));
                        writer.WriteElementString(XmlElementType, mapTile.Type);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();

                    writer.WriteEndElement();
                }
            }
        }

        /// <summary>
        /// Gets the image for the map tile of the specified type.
        /// </summary>
        /// <param name="tileType">Type of the tile to get the image for.</param>
        /// <returns>Image for the map tile of the specified type.</returns>
        public BitmapImage GetTileImage(string tileType)
        {
            return this.tileImages[tileType];
        }

        public void OnBrushSelected(string brush)
        {
            this.currentBrush = this.tileTypes[brush];
        }

        public void OnTileClicked(Vector2I position)
        {
            // Early out if no brush selected.
            if (this.currentBrush == null)
            {
                return;
            }

            // Modify map model.
            this.map[position].Type = this.currentBrush.Name;

            // Update canvas.
            this.mainWindow.UpdateMapCanvas(position, this.tileImages[this.currentBrush.Name]);
        }

        #endregion

        #region Methods

        private void OnActivated(object sender, EventArgs e)
        {
            this.mainWindow = (MainWindow)this.MainWindow;
            this.mainWindow.SetMapTileTypes(this.tileTypes.Keys);
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            // Define map tile types.
            this.tileTypes = new Dictionary<string, MapTileType>();

            var desert = new MapTileType(3, "Desert");
            var water = new MapTileType(5, "Water");
            var grass = new MapTileType(1, "Grass");

            this.tileTypes.Add(desert.Name, desert);
            this.tileTypes.Add(water.Name, water);
            this.tileTypes.Add(grass.Name, grass);

            // Load sprites.
            this.tileImages = new Dictionary<string, BitmapImage>();

            foreach (var tileType in this.tileTypes.Values)
            {
                var imageUri = "pack://application:,,,/Resources/MapTiles/" + tileType.Name + ".png";

                BitmapImage tileImage = new BitmapImage();
                tileImage.BeginInit();
                tileImage.UriSource = new Uri(imageUri);
                tileImage.EndInit();

                this.tileImages.Add(tileType.Name, tileImage);
            }
        }

        /// <summary>
        /// Shows an error message with the specified title and text.
        /// </summary>
        /// <param name="title">Title of the error message.</param>
        /// <param name="text">Text of the error message.</param>
        private void ShowErrorMessage(string title, string text)
        {
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Cancel);
        }

        /// <summary>
        /// Passes the current map to the canvas for rendering.
        /// </summary>
        private void UpdateMapCanvas()
        {
            this.mainWindow.UpdateMapCanvas(this.map);
        }

        #endregion
    }
}