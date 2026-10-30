# Apply after qpdf has created its targets, without modifying its source tree.
cmake_language(DEFER CALL set_target_properties libqpdf PROPERTIES
    OUTPUT_NAME pdfxmpreader_qpdf)
