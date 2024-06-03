InkSight is an alternative Checking Tool designed for the title "INKSIGHT : STREAMLINED GRADING WITH OPTICAL CHARACTER RECOGNITION AND IMAGE PROCESSING FOR HANDWRITTEN ASSESSMENTS".

InkSight is a Windows Forms application that aims to standardize handwritten test checking, creating an alternative to manual checking.


Requirements - requires at least .NET framework 4.8, Azure OCR Read API (subscription not included but temporary key is hardcoded in demo)


Features 

- Allows importation of image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)
- Allows creation of user-input answer keys; importation of .txt files as answer key  
- Process images using Azure OCR Read API for text extraction
- Optional Image preprocessing Operations using OpenCV
- Text post-processing to trim Numbering, e.g cases like 1 1. 1 . 1.) 
- Tabulate text readings to table for direct comparison, create score based on readings and answer key
- Provide option to create lists for sections and students.
- Export Student, Section, Total Score as an CSV file.

 Limitations 

- Only uploads images one at a time.
- Preprocessing is only applicable to darker papers and might create unwanted results.
- Readings may be subject to error on larger number of tests.
- Cannot evaluate number answers due to text trimming post-process 
