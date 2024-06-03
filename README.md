InkSight: Streamlined Grading with Optical Character Recognition and Image Processing for Handwritten Assessments
InkSight is a Windows Forms application designed to standardize handwritten test checking, providing an alternative to manual grading.

Requirements
.NET Framework 4.8 or higher
Azure OCR Read API (Subscription not included; a temporary key is hardcoded in the demo)
Features
Image Importation: Supports importing image files in the following formats: *.png, *.jpg, *.jpeg, *.gif, *.bmp.
Answer Key Creation: Allows creation of user-input answer keys and the importation of answer keys from .txt files.
OCR Processing: Uses Azure OCR Read API to extract text from images.
Image Preprocessing: Optional image preprocessing operations using OpenCV.
Text Post-Processing: Trims numbering formats (e.g., 1, 1., 1., 1.)).
Comparison and Scoring: Tabulates text readings into a table for direct comparison and scoring based on the answer key.
Student and Section Management: Provides options to create lists for sections and students.
Export Functionality: Exports student, section, and total scores as a CSV file.
Installation
Ensure that you have .NET Framework 4.8 or higher installed.
Clone the repository:
bash
Copy code
git clone https://github.com/yourusername/inksight.git
Open the solution in Visual Studio.
Update the Azure OCR Read API key in the configuration file.
Build and run the application.
Usage
Import Images: Use the import function to load image files containing handwritten assessments.
Create Answer Key: Manually input answer keys or import them from a .txt file.
Process Images: Run the OCR processing to extract text from the imported images.
Compare and Score: The application will compare extracted text with the answer key and generate scores.
Manage Students and Sections: Create and manage lists for different sections and students.
Export Results: Export the scores and other relevant data as a CSV file.
