
using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Shapes;
using System.Text;
using OpenCvSharp.WpfExtensions;
using OpenCvSharp;
using System.Collections.Generic;
using System.Windows.Data;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Microsoft.VisualBasic;


namespace InkSight
{
    public partial class MainWindow : System.Windows.Window
    {
        private bool isDragging;
        private System.Windows.Point dragStartPoint;
        private BitmapImage originalImage;
        private BitmapImage originalImageSource;
        private string selectedZipFilePath;
        private Ellipse ellipseMove;
        private bool isEllipseDragging;
        private System.Windows.Point ellipseDragStartPoint;
        private BitmapSource croppedImage;

        private string selectedStudentName;
        private string selectedSectionName;
        private string answerKeyZipName;
        private string subscriptionKey;


        public MainWindow()
        {
            InitializeComponent();
            InitializeCropRectangle();
            PopulateAnswerKeyComboBox();
            PopulateComboBoxSectionList();
        }

        private void BtnUploadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtImagePath.Text = openFileDialog.FileName;

                try
                {
                    Uri uri = new Uri(openFileDialog.FileName);
                    originalImage = new BitmapImage(uri);
                    originalImageSource = new BitmapImage(uri);
                    imgDisplay.Source = originalImage;

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading the image: " + ex.Message);
                }
                cropRectangle.Visibility = Visibility.Visible;
                ellipseMove.Visibility = Visibility.Visible;

            }
        }

        private void BtnUploadAnswerKey_Click(object sender, RoutedEventArgs e)
        {
            string answerKeyName = "";

            while (true)
            {
                answerKeyName = Interaction.InputBox("Enter answer key name:", "Answer Key Name");

                if (answerKeyName == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(answerKeyName))
                {
                    break;
                }

                MessageBox.Show("Answer key name cannot be empty.");
            }

            int numQuestions = 0;
            while (true)
            {
                string numQuestionsInput = Interaction.InputBox("Input number of questions:", "Number of Questions");

                if (numQuestionsInput == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(numQuestionsInput) && int.TryParse(numQuestionsInput, out numQuestions) && numQuestions > 0)
                {
                    break;
                }

                MessageBox.Show("Please enter a valid positive integer for the number of questions.");
            }

            string answerKeyContent = "";
            for (int i = 1; i <= numQuestions; i++)
            {
                string answer = "";
                while (true)
                {
                    answer = Interaction.InputBox($"Answer for Question {i}:", $"Question {i}");

                    if (answer == null)
                    {
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(answer) && !int.TryParse(answer, out _)) // Valid input
                    {
                        break;
                    }

                    MessageBox.Show($"Answer for Question {i} cannot be empty or a number.");
                }

                answerKeyContent += $"{answer}\n";
            }

            try
            {
                string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                System.IO.Directory.CreateDirectory(tempDir);
                string answerKeyFilePath = System.IO.Path.Combine(tempDir, answerKeyName + ".txt");

                File.WriteAllText(answerKeyFilePath, answerKeyContent);

                string answerKeysFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnswerKeys");
                if (!Directory.Exists(answerKeysFolder))
                {
                    Directory.CreateDirectory(answerKeysFolder);
                }

                string zipFilePath = System.IO.Path.Combine(answerKeysFolder, answerKeyName + ".zip");

                if (File.Exists(zipFilePath))
                {
                    MessageBoxResult result = MessageBox.Show($"The file {answerKeyName}.zip already exists. Do you want to overwrite it?", "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }

                    File.Delete(zipFilePath);
                }

                using (ZipArchive zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(answerKeyFilePath, answerKeyName + ".txt");
                }

                string zipPassFolderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZipPass");
                if (!Directory.Exists(zipPassFolderPath))
                {
                    Directory.CreateDirectory(zipPassFolderPath);
                }

                string keyMapFilePath = System.IO.Path.Combine(zipPassFolderPath, "KeyMap.txt");
                using (StreamWriter writer = File.AppendText(keyMapFilePath))
                {
                    writer.WriteLine($"{answerKeyName}");
                }

                Directory.Delete(tempDir, true);

                MessageBox.Show("Answer key uploaded and saved successfully.");
                PopulateAnswerKeyComboBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving answer key: {ex.Message}");
            }
        }


        private void BtnImportAnswerKey_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(selectedFilePath);
                string answerKeysFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnswerKeys");
                string zipFilePath = System.IO.Path.Combine(answerKeysFolder, fileNameWithoutExtension + ".zip");

                try
                {
                    if (File.Exists(zipFilePath))
                    {
                        MessageBoxResult result = MessageBox.Show($"The file {fileNameWithoutExtension}.zip already exists. Do you want to overwrite it?", "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }

                        File.Delete(zipFilePath);
                    }

                    if (!Directory.Exists(answerKeysFolder))
                    {
                        Directory.CreateDirectory(answerKeysFolder);
                    }

                    string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);

                    string destinationTxtFilePath = System.IO.Path.Combine(tempDir, fileNameWithoutExtension + ".txt");

                    string[] txtLines = File.ReadAllLines(selectedFilePath);
                    StringBuilder trimmedTextBuilder = new StringBuilder();

                    foreach (string line in txtLines)
                    {
                        string trimmedLine = Regex.Replace(line, @"^\s*\d+\.\s*", "");
                        trimmedTextBuilder.AppendLine(trimmedLine.Trim());
                    }

                    File.WriteAllText(destinationTxtFilePath, trimmedTextBuilder.ToString());

                    using (ZipArchive zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                    {
                        zip.CreateEntryFromFile(destinationTxtFilePath, fileNameWithoutExtension + ".txt");
                    }

                    Directory.Delete(tempDir, true);

                    MessageBox.Show("Answer key imported and saved successfully.");
                    PopulateAnswerKeyComboBox();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing answer key: {ex.Message}");
                }
            }
        }




        private void ComboBoxAnswerKeys_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxAnswerKeys.SelectedItem != null)
            {
                selectedZipFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnswerKeys", comboBoxAnswerKeys.SelectedItem.ToString());
                lblSelectedZipFilePath.Content = selectedZipFilePath;
                answerKeyZipName = comboBoxAnswerKeys.SelectedItem.ToString();
            }
        }



        private void PopulateAnswerKeyComboBox()
        {
            string answerKeysFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnswerKeys");
            if (Directory.Exists(answerKeysFolder))
            {
                string[] zipFiles = Directory.GetFiles(answerKeysFolder, "*.zip");
                comboBoxAnswerKeys.Items.Clear();
                foreach (string zipFile in zipFiles)
                {
                    comboBoxAnswerKeys.Items.Add(System.IO.Path.GetFileName(zipFile));
                }
            }
        }

        private void PopulateComboBoxSectionList()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string sectionsFolder = System.IO.Path.Combine(baseDirectory, "class_sections");

                if (!Directory.Exists(sectionsFolder))
                {
                    Directory.CreateDirectory(sectionsFolder);
                }

                string[] sectionFiles = Directory.GetFiles(sectionsFolder, "*.txt");

                comboBoxSectionList.Items.Clear();
                foreach (string filePath in sectionFiles)
                {
                    string sectionName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    comboBoxSectionList.Items.Add(sectionName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error populating section list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        private void ImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (cropRectangle.Visibility == Visibility.Visible)
            {
                double canvasWidth = ImageCanvas.ActualWidth;
                double canvasHeight = ImageCanvas.ActualHeight;
                double newLeft = Canvas.GetLeft(cropRectangle);
                double newTop = Canvas.GetTop(cropRectangle);

                if (newLeft + cropRectangle.Width > canvasWidth)
                {
                    cropRectangle.Width = canvasWidth - newLeft;
                }

                if (newTop + cropRectangle.Height > canvasHeight)
                {
                    cropRectangle.Height = canvasHeight - newTop;
                }

                if (newLeft < 0)
                {
                    cropRectangle.Width += newLeft;
                    Canvas.SetLeft(cropRectangle, 0);
                }

                if (newTop < 0)
                {
                    cropRectangle.Height += newTop;
                    Canvas.SetTop(cropRectangle, 0);
                }
            }
        }


        private void InitializeCropRectangle()
        {
            cropRectangle = new System.Windows.Shapes.Rectangle
            {
                Stroke = System.Windows.Media.Brushes.Red,
                StrokeThickness = 2,
                Fill = System.Windows.Media.Brushes.Transparent,
                Width = 100,
                Height = 100,
                Visibility = Visibility.Visible
            };

            Canvas.SetLeft(cropRectangle, 50);
            Canvas.SetTop(cropRectangle, 50);

            cropRectangle.MouseLeftButtonDown += CropRectangle_MouseLeftButtonDown;
            cropRectangle.MouseLeftButtonUp += CropRectangle_MouseLeftButtonUp;
            cropRectangle.MouseMove += CropRectangle_MouseMove;

            ImageCanvas.Children.Add(cropRectangle);

            ellipseMove = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = System.Windows.Media.Brushes.Blue,
                Stroke = System.Windows.Media.Brushes.Black,
                StrokeThickness = 2,
                Opacity = 0.1
            };



            Canvas.SetLeft(ellipseMove, Canvas.GetLeft(cropRectangle) + cropRectangle.Width / 2 - ellipseMove.Width / 2);
            Canvas.SetTop(ellipseMove, Canvas.GetTop(cropRectangle) + cropRectangle.Height / 2 - ellipseMove.Height / 2);


            Canvas.SetZIndex(ellipseMove, 1);

            ellipseMove.MouseLeftButtonDown += EllipseMove_MouseLeftButtonDown;
            ellipseMove.MouseLeftButtonUp += EllipseMove_MouseLeftButtonUp;
            ellipseMove.MouseMove += EllipseMove_MouseMove;



            ImageCanvas.Children.Add(ellipseMove);



        }


        private void CropRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            dragStartPoint = e.GetPosition(ImageCanvas);
            cropRectangle.CaptureMouse();
        }

        private void CropRectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            cropRectangle.ReleaseMouseCapture();
        }

        private void CropRectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                System.Windows.Point currentPosition = e.GetPosition(ImageCanvas);

                double deltaX = currentPosition.X - dragStartPoint.X;
                double deltaY = currentPosition.Y - dragStartPoint.Y;

                double newWidth = cropRectangle.Width + deltaX;
                double newHeight = cropRectangle.Height + deltaY;

                double newLeft = Canvas.GetLeft(cropRectangle);
                double newTop = Canvas.GetTop(cropRectangle);

                if (newWidth > 0 && newLeft + newWidth < ImageCanvas.ActualWidth)
                {
                    cropRectangle.Width = newWidth;
                }

                if (newHeight > 0 && newTop + newHeight < ImageCanvas.ActualHeight)
                {
                    cropRectangle.Height = newHeight;
                }

                dragStartPoint = currentPosition;

                UpdateEllipsePosition();
            }
        }

        private void EllipseMove_MouseMove(object sender, MouseEventArgs e)
        {
            if (isEllipseDragging)
            {
                System.Windows.Point currentPosition = e.GetPosition(ImageCanvas);

                double deltaX = currentPosition.X - ellipseDragStartPoint.X;
                double deltaY = currentPosition.Y - ellipseDragStartPoint.Y;

                double newLeft = Canvas.GetLeft(cropRectangle) + deltaX;
                double newTop = Canvas.GetTop(cropRectangle) + deltaY;

                if (newLeft >= 0 && newLeft + cropRectangle.Width <= ImageCanvas.ActualWidth)
                {
                    Canvas.SetLeft(cropRectangle, newLeft);
                    Canvas.SetLeft(ellipseMove, newLeft + cropRectangle.Width / 2 - ellipseMove.Width / 2);
                }

                if (newTop >= 0 && newTop + cropRectangle.Height <= ImageCanvas.ActualHeight)
                {
                    Canvas.SetTop(cropRectangle, newTop);
                    Canvas.SetTop(ellipseMove, newTop + cropRectangle.Height / 2 - ellipseMove.Height / 2);

                }

                ellipseDragStartPoint = currentPosition;
            }
        }



        private void EllipseMove_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isEllipseDragging = true;
            ellipseDragStartPoint = e.GetPosition(ImageCanvas);
            ellipseMove.CaptureMouse();
        }

        private void EllipseMove_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isEllipseDragging = false;
            ellipseMove.ReleaseMouseCapture();
        }


        private void UpdateEllipsePosition()
        {
            Canvas.SetLeft(ellipseMove, Canvas.GetLeft(cropRectangle) + cropRectangle.Width / 2 - ellipseMove.Width / 2);
            Canvas.SetTop(ellipseMove, Canvas.GetTop(cropRectangle) + cropRectangle.Height / 2 - ellipseMove.Height / 2);
        }

        private void CropImage()
        {
            if (imgDisplay.Source != null && imgDisplay.ActualWidth > 0 && imgDisplay.ActualHeight > 0)
            {
                BitmapSource displayedImage = (BitmapSource)imgDisplay.Source;

                double scaleX = displayedImage.PixelWidth / imgDisplay.ActualWidth;
                double scaleY = displayedImage.PixelHeight / imgDisplay.ActualHeight;

                double x = Canvas.GetLeft(cropRectangle) * scaleX;
                double y = Canvas.GetTop(cropRectangle) * scaleY;
                double width = cropRectangle.Width * scaleX;
                double height = cropRectangle.Height * scaleY;

                if (x >= 0 && y >= 0 && width > 0 && height > 0 &&
                    x + width <= displayedImage.PixelWidth && y + height <= displayedImage.PixelHeight)
                {
                    CroppedBitmap croppedBitmap = new CroppedBitmap(displayedImage, new Int32Rect((int)x, (int)y, (int)width, (int)height));

                    croppedImage = croppedBitmap;

                    imgDisplay.Source = croppedBitmap;

                    cropRectangle.Visibility = Visibility.Collapsed;
                    ellipseMove.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnCrop_Click(object sender, RoutedEventArgs e)
        {
            CropImage();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (originalImageSource != null)
            {
                imgDisplay.Source = originalImageSource;

                cropRectangle.Visibility = Visibility.Visible;
                ellipseMove.Visibility = Visibility.Visible;
            }
        }

        private void BtnRotateNeg90(object sender, RoutedEventArgs e)
        {
            {
                if (imgDisplay.Source == null || !(imgDisplay.Source is BitmapSource bitmapSource))
                {
                    MessageBox.Show("No image loaded.");
                    return;
                }

                TransformedBitmap rotatedBitmap = new TransformedBitmap(bitmapSource,
                    new RotateTransform(-90));
                imgDisplay.Source = rotatedBitmap;
            }
        }

        private void BtnRotate90(object sender, RoutedEventArgs e)
        {

            if (imgDisplay.Source == null || !(imgDisplay.Source is BitmapSource bitmapSource))
            {
                MessageBox.Show("No image loaded.");
                return;
            }


            TransformedBitmap rotatedBitmap = new TransformedBitmap(bitmapSource,
                new RotateTransform(90));

            imgDisplay.Source = rotatedBitmap;
        }
        private void BtnPreprocess_Click(object sender, RoutedEventArgs e)
        {
            if (cropRectangle.Visibility == Visibility.Collapsed)
            {
                MessageBox.Show("Image has already been cropped");
            }
            else
            {
                CropImage();
            }

            try
            {
                // Check if an image is loaded
                if (imgDisplay.Source == null || !(imgDisplay.Source is BitmapSource bitmapSource))
                {
                    MessageBox.Show("No image loaded.");
                    return;
                }

                // Convert BitmapSource to Mat (assuming OpenCVSharp is used for processing)
                Mat originalImage = BitmapSourceConverter.ToMat(bitmapSource);

                if (originalImage.Empty())
                {
                    MessageBox.Show("Failed to convert image to Mat.");
                    return;
                }

                Mat grayImage = new Mat();
                Cv2.CvtColor(originalImage, grayImage, ColorConversionCodes.BGR2GRAY);

                Mat claheImage = new Mat();
                using (CLAHE clahe = Cv2.CreateCLAHE(3.0, new OpenCvSharp.Size(24, 24)))
                {
                    clahe.Apply(grayImage, claheImage);
                }

                Mat brightenedImage = new Mat();
                claheImage.ConvertTo(brightenedImage, -1, 1, 80); // Increase brightness by adding 80 to pixel values

                Mat sharpenedImage = new Mat();
                Cv2.GaussianBlur(brightenedImage, sharpenedImage, new OpenCvSharp.Size(3, 3), 3);
                Cv2.AddWeighted(brightenedImage, 1.5, sharpenedImage, -0.3, 0, sharpenedImage);

                Mat denoisedImage = new Mat();
                Cv2.GaussianBlur(sharpenedImage, denoisedImage, new OpenCvSharp.Size(5, 5), 0);

                Mat thresholdedImage = new Mat();
                Cv2.AdaptiveThreshold(
                    denoisedImage,
                    thresholdedImage,
                    maxValue: 255,
                    adaptiveMethod: AdaptiveThresholdTypes.GaussianC,
                    thresholdType: ThresholdTypes.Binary,
                    blockSize: 9,
                    c: 2
                );

                
                BitmapSource processedBitmap = BitmapSourceConverter.ToBitmapSource(thresholdedImage);

                
                imgDisplay.Source = processedBitmap;

              

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preprocessing image: {ex.Message}");
            }
        }















        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxAnswerKeys.SelectedItem == null)
            {
                MessageBox.Show("No Answer Key Selected.");
                return;
            }

            try
            {

                if (imgDisplay.Source == null)
                {
                    MessageBox.Show("No image loaded.");
                    return;
                }


                BitmapSource bitmapSource = (BitmapSource)imgDisplay.Source;

                List<TestResult> results = await PerformOCR(bitmapSource);

                LoadAnswerKey(results);

                dataGridResults.ItemsSource = results;
                UpdateTotalScore(results);
                UpdateAnswerKeyTable(results);


                MessageBox.Show("Image processed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing image: {ex.Message}");
            }

            if (cropRectangle.Visibility == Visibility.Collapsed)
            {
                imgDisplay.Source = originalImageSource;
                cropRectangle.Visibility = Visibility.Visible;
                ellipseMove.Visibility = Visibility.Visible;
            }
        }

        private async Task<List<TestResult>> PerformOCR(BitmapSource bitmapSource)
        {
            List<TestResult> results = new List<TestResult>();

            try
            {
                // Hardcode Key for Demonstration purposes
                subscriptionKey = "502b79a8ca5c4de688ef2211f72ed03f";
                string endpoint = "https://ocr-thesis.cognitiveservices.azure.com/";
                ComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey))
                {
                    Endpoint = endpoint
                };


                byte[] imageBytes = ImageToByteArray(bitmapSource);


                using (MemoryStream stream = new MemoryStream(imageBytes))
                {
                    ReadInStreamHeaders headers = await client.ReadInStreamAsync(stream);
                    string operationId = headers.OperationLocation.Split('/').Last();


                    ReadOperationResult result;
                    do
                    {
                        result = await client.GetReadResultAsync(Guid.Parse(operationId));
                        await Task.Delay(1000);
                    } while (result.Status == OperationStatusCodes.Running || result.Status == OperationStatusCodes.NotStarted);


                    if (result.Status == OperationStatusCodes.Succeeded)
                    {
                        int lineNumber = 1;

                        foreach (var pageResult in result.AnalyzeResult.ReadResults)
                        {
                            foreach (var line in pageResult.Lines)
                            {

                                StringBuilder lineText = new StringBuilder();
                                foreach (var word in line.Words)
                                {
                                    lineText.Append(word.Text + " ");
                                }


                                string cleanedLineText = Regex.Replace(lineText.ToString().Trim(), @"^\d+(\.|\.\)|\))\s*", "");
                                cleanedLineText = Regex.Replace(cleanedLineText, @"^\d+\s*", "").Trim();
                                cleanedLineText = Regex.Replace(cleanedLineText, @"^[^\p{L}\d]+\s*", "").Trim();
                                cleanedLineText = Regex.Replace(cleanedLineText, @"^\p{P}+\s", "").Trim();
                                cleanedLineText = Regex.Replace(cleanedLineText, @"^\d{1,2}\.\s*", "").Trim();
                                cleanedLineText = Regex.Replace(cleanedLineText, @"^\p{P}(\s\.)?", "").Trim();
                                cleanedLineText = Regex.Replace(cleanedLineText, @"^\d+\s[.,]", "").Trim();
                                cleanedLineText = Regex.Replace(cleanedLineText, @"^\d+[,%/]", "").Trim();
                                cleanedLineText = Regex.Replace(cleanedLineText, @"^\p{L}\p{P}", "").Trim();

                                if (string.IsNullOrWhiteSpace(cleanedLineText))
                                {
                                    continue;
                                }


                                TestResult testResult = new TestResult
                                {
                                    No = lineNumber++,
                                    Answer = cleanedLineText,
                                    Score = 0,
                                    AnswersList = new List<string>()
                                };

                                results.Add(testResult);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("OCR operation did not succeed.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing OCR: {ex.Message}");
            }

            return results;
        }


        private byte[] ImageToByteArray(BitmapSource bitmapSource)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        private List<TestResult> ReadAnswerKey(string zipFilePath)
        {
            List<TestResult> testResults = new List<TestResult>();

            try
            {
                using (ZipArchive zip = ZipFile.OpenRead(zipFilePath))
                {

                    List<string> lines = new List<string>();

                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        using (StreamReader reader = new StreamReader(entry.Open()))
                        {
                            string entryContent = reader.ReadToEnd().Trim();
                            if (!string.IsNullOrWhiteSpace(entryContent))
                            {

                                lines.AddRange(entryContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries));
                            }
                        }
                    }

                    foreach (string line in lines)
                    {

                        string[] parts = line.Split(',');

                        int no = testResults.Count + 1;

                        List<string> answersList = new List<string>();
                        foreach (string part in parts)
                        {
                            answersList.Add(part.Trim());
                        }


                        string formattedAnswerKey = string.Join(", ", answersList);

                        TestResult result = new TestResult
                        {
                            No = no,
                            AnswersList = answersList,
                            AnswerKey = formattedAnswerKey
                        };

                        testResults.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading ZIP file '{zipFilePath}': {ex.Message}");
            }

            return testResults;
        }





        private void LoadAnswerKey(List<TestResult> results)
        {
            if (comboBoxAnswerKeys.SelectedItem == null)
            {
                MessageBox.Show("No Answer Key Selected.");
                return;
            }
            if (results == null || results.Count == 0)
            {
                MessageBox.Show("No OCR results to load answer key.");
                return;
            }

            string selectedAnswerKeyFileName = comboBoxAnswerKeys.SelectedItem.ToString();
            string answerKeysFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnswerKeys");
            string selectedZipFilePath = System.IO.Path.Combine(answerKeysFolder, selectedAnswerKeyFileName);

            try
            {
                List<TestResult> answerKeyResults = ReadAnswerKey(selectedZipFilePath);


                foreach (TestResult result in results)
                {
                    TestResult matchingAnswerKey = answerKeyResults.FirstOrDefault(r => r.No == result.No);

                    if (matchingAnswerKey != null)
                    {
                        result.AnswerKey = matchingAnswerKey.AnswerKey;
                        result.AnswersList = matchingAnswerKey.AnswersList;

                        bool isCaseSensitiveChecked = chkIsCaseSensitive.IsChecked ?? false;
                        bool isAnswerCorrect = false;

                        foreach (string answer in matchingAnswerKey.AnswersList)
                        {
                            if (isCaseSensitiveChecked)
                            {
                                if (string.Equals(result.Answer.Trim(), answer.Trim(), StringComparison.Ordinal))
                                {
                                    isAnswerCorrect = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (string.Equals(result.Answer.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    isAnswerCorrect = true;
                                    break;
                                }
                            }
                        }

                        result.Score = isAnswerCorrect ? 1 : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading answer key: {ex.Message}");
            }
        }



        private void UpdateAnswerKeyTable(List<TestResult> results)
        {

            dataGridResults.Items.Clear();


            dataGridResults.Columns.Clear();
            dataGridResults.Columns.Add(new DataGridTextColumn { Header = "No", Binding = new Binding("No") });
            dataGridResults.Columns.Add(new DataGridTextColumn { Header = "OCR Answer", Binding = new Binding("Answer") });
            dataGridResults.Columns.Add(new DataGridTextColumn { Header = "Answer Key", Binding = new Binding("FormattedAnswerKey") });
            dataGridResults.Columns.Add(new DataGridTextColumn { Header = "Score", Binding = new Binding("Score") });

            foreach (var result in results)
            {

                string formattedAnswerKey = string.Join(", ", result.AnswersList);


                var displayResult = new
                {
                    result.No,
                    result.Answer,
                    FormattedAnswerKey = formattedAnswerKey,
                    result.Score
                };

                dataGridResults.Items.Add(displayResult);
            }
        }


        private string UpdateTotalScore(List<TestResult> results)
        {
            try
            {
                int totalScoreValue = results.Sum(r => r.Score);
                int totalCount = results.Count;

                string totalScoreText = $"{totalScoreValue}|{totalCount}";
                txtTotalScore.Text = totalScoreText;

                return totalScoreText;
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                Console.WriteLine($"Error updating total score: {ex.Message}");
                return "Error";
            }
        }

        public class TestResult
        {
            public int No { get; set; }
            public string Answer { get; set; }
            public string AnswerKey { get; set; }
            public int Score { get; set; }
            public List<string> AnswersList { get; set; } = new List<string>(); // Initialize the list
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            string selectedAnswerKeyFileName = comboBoxAnswerKeys.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedAnswerKeyFileName))
            {
                MessageBox.Show("No answer key selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string answerKeysFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnswerKeys");
            string selectedZipFilePath = System.IO.Path.Combine(answerKeysFolder, selectedAnswerKeyFileName);

            try
            {
                List<TestResult> testResults = ReadAnswerKey(selectedZipFilePath);

                if (testResults.Count > 0)
                {
                    StringBuilder messageContent = new StringBuilder();
                    foreach (TestResult result in testResults)
                    {
                        messageContent.AppendLine($"No {result.No}: - Answer Key: {result.AnswerKey}");
                    }

                    MessageBox.Show(messageContent.ToString(), "Answer Key Contents", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No test results found in the ZIP file.", "Answer Key Contents", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading ZIP file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void btnCreateSection_Click(object sender, RoutedEventArgs e)
        {

            string sectionName = Interaction.InputBox("Enter Section Name:", "Create Section");

            if (string.IsNullOrEmpty(sectionName))
            {
                MessageBox.Show("Section name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string sectionsFolder = System.IO.Path.Combine(baseDirectory, "class_sections");
                string filePath = System.IO.Path.Combine(sectionsFolder, $"{sectionName}.txt");


                if (!Directory.Exists(sectionsFolder))
                {
                    Directory.CreateDirectory(sectionsFolder);
                }


                if (File.Exists(filePath))
                {
                    MessageBoxResult result = MessageBox.Show($"Section '{sectionName}' already exists. Do you want to overwrite it?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }


                File.WriteAllText(filePath, string.Empty);

                MessageBox.Show($"Section '{sectionName}' created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);


                comboBoxSectionList.Items.Add(sectionName);
                comboBoxSectionList.SelectedItem = sectionName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating section: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        private void comboBoxSectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            string selectedSection = comboBoxSectionList.SelectedItem as string;

            if (selectedSection != null)
            {
                selectedSectionName = selectedSection;

                try
                {

                    string sectionsFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "class_sections");
                    string sectionFilePath = System.IO.Path.Combine(sectionsFolder, $"{selectedSection}.txt");


                    if (File.Exists(sectionFilePath))
                    {

                        string[] studentNames = File.ReadAllLines(sectionFilePath, Encoding.UTF8);


                        comboBoxStudentList.ItemsSource = studentNames;
                    }
                    else
                    {

                        comboBoxStudentList.ItemsSource = null;
                        MessageBox.Show($"Section file '{sectionFilePath}' not found or empty.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading section file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void comboBoxStudentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedStudent = comboBoxStudentList.SelectedItem as string;

            if (!string.IsNullOrEmpty(selectedStudent))
            {
                selectedStudentName = selectedStudent;


            }
        }



        private List<string> GetAvailableSections()
        {
            string sectionsFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "class_sections");

            if (!Directory.Exists(sectionsFolder))
            {
                return new List<string>();
            }

            string[] sectionFiles = Directory.GetFiles(sectionsFolder, "*.txt");

            List<string> sectionNames = new List<string>();
            foreach (string filePath in sectionFiles)
            {
                string sectionName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                sectionNames.Add(sectionName);
            }

            return sectionNames;
        }

        private void btnAddNewStudent_Click(object sender, RoutedEventArgs e)
        {
            List<string> availableSections = GetAvailableSections();


            int sectionIndex = -1;
            while (sectionIndex < 0 || sectionIndex >= availableSections.Count)
            {
                string sectionPrompt = "Select a section to add students:";
                for (int i = 0; i < availableSections.Count; i++)
                {
                    sectionPrompt += $"\n{i + 1}. {availableSections[i]}";
                }

                string sectionInput = Interaction.InputBox(sectionPrompt, "Select Section");
                if (string.IsNullOrWhiteSpace(sectionInput))
                {
                    MessageBox.Show("Section selection cannot be empty.");
                    return;
                }

                if (!int.TryParse(sectionInput, out sectionIndex) || sectionIndex < 1 || sectionIndex > availableSections.Count)
                {
                    MessageBox.Show("Please enter a valid section number.");
                    sectionIndex = -1;
                }
                else
                {
                    sectionIndex--;
                }
            }

            string selectedSection = availableSections[sectionIndex];

            int numStudents = 0;
            while (numStudents <= 0)
            {
                string numStudentsInput = Interaction.InputBox("Enter number of students to add:", "Number of Students");
                if (string.IsNullOrWhiteSpace(numStudentsInput))
                {
                    MessageBox.Show("Input cannot be empty.");
                    return;
                }
                if (!int.TryParse(numStudentsInput, out numStudents) || numStudents <= 0)
                {
                    MessageBox.Show("Please enter a valid positive integer for the number of students.");
                }
            }

            try
            {
                // Construct file path to the selected section's text file
                string sectionsFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "class_sections");
                string sectionFilePath = System.IO.Path.Combine(sectionsFolder, $"{selectedSection}.txt");

                // Add student names to a list
                List<string> studentNames = new List<string>();
                for (int i = 0; i < numStudents; i++)
                {
                    string newStudentName = Interaction.InputBox($"Enter name for Student {i + 1}:", "Student Name");
                    if (!string.IsNullOrWhiteSpace(newStudentName))
                    {
                        studentNames.Add(newStudentName);
                    }
                }


                List<string> existingStudentNames = new List<string>();
                if (File.Exists(sectionFilePath))
                {
                    existingStudentNames.AddRange(File.ReadAllLines(sectionFilePath));
                }

                existingStudentNames.AddRange(studentNames);

                existingStudentNames.Sort();

                using (StreamWriter writer = new StreamWriter(sectionFilePath))
                {
                    foreach (string studentName in existingStudentNames)
                    {
                        writer.WriteLine(studentName);
                    }
                }

                comboBoxSectionList_SelectionChanged(null, null);

                MessageBox.Show($"{numStudents} student(s) added to section '{selectedSection}'.", "Students Added", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding students: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void CompileGrade_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string testGradesFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_grades");
                if (!Directory.Exists(testGradesFolder))
                {
                    Directory.CreateDirectory(testGradesFolder);
                }

                string answerKeyZipNameFolder = System.IO.Path.Combine(testGradesFolder, answerKeyZipName);
                if (!Directory.Exists(answerKeyZipNameFolder))
                {
                    Directory.CreateDirectory(answerKeyZipNameFolder);
                }

                string csvFilePath = System.IO.Path.Combine(answerKeyZipNameFolder, "grades.csv");

                string totalScoreText = txtTotalScore.Text;

                string selectedStudent = selectedStudentName;
                string selectedSection = selectedSectionName;


                List<string> lines;

                if (File.Exists(csvFilePath))
                {
                    lines = File.ReadAllLines(csvFilePath).ToList();

                    string headers = lines.FirstOrDefault();
                    lines.Remove(headers);

                    bool foundDuplicate = false;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string[] fields = lines[i].Split(',');
                        if (fields.Length >= 2 && fields[0].Trim() == selectedStudent && fields[1].Trim() == selectedSection)
                        {

                            lines[i] = $"{selectedStudent},{selectedSection},{totalScoreText}";
                            foundDuplicate = true;
                            break;
                        }
                    }

                    if (!foundDuplicate)
                    {
                        lines.Add($"{selectedStudent},{selectedSection},{totalScoreText}");
                    }

                    lines = lines.OrderBy(line =>
                    {
                        string[] parts = line.Split(',');
                        return parts[1];
                    }).ThenBy(line =>
                    {
                        string[] parts = line.Split(',');
                        return parts[0];

                    }).ToList();

                    lines.Insert(0, headers);
                }
                else
                {
                    lines = new List<string>
            {
                "NAME,SECTION,TOTAL SCORE",
                $"{selectedStudent},{selectedSection},{totalScoreText}"
            };
                }

                File.WriteAllLines(csvFilePath, lines);

                MessageBox.Show("Grade compiled successfully and saved to CSV.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error compiling grade: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AzureApiPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {

                subscriptionKey = passwordBox.Password;


            }


        }
    }
}

