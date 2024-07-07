# InkSight: Streamlined Grading with Optical Character Recognition and Image Processing for Handwritten Assessments

**InkSight** is a Windows Forms application designed to standardize handwritten test checking, providing an alternative to manual grading.

## Requirements

- .NET Framework 4.8 or higher
- Azure OCR Read API (Subscription not included)

## Features

- **Image Importation**: Supports importing image files in the following formats: *.png, *.jpg, *.jpeg, *.gif, *.bmp.
- **Answer Key Creation**: Allows creation of user-input answer keys and the importation of answer keys from .txt files.
- **OCR Processing**: Uses Azure OCR Read API to extract text from images.

- **Text Cleaning**: Trims numbering formats (e.g., 1, 1., 1., 1.)).
- **Comparison and Scoring**: Tabulates text readings into a table for direct comparison and scoring based on the answer key.
- **Student and Section Management**: Provides options to create lists for sections and students.
- **Export Functionality**: Exports student, section, and total scores as a CSV file.

## Installation

1. Ensure that you have .NET Framework 4.8 or higher installed.
2. Clone the repository:
    ```bash
    git clone https://github.com/yourusername/inksight.git
    ```
3. Open the solution in Visual Studio.
4. Update the Azure OCR Read API key in the configuration file.
5. Build and run the application.
