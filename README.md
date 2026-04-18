**DesktopSearchApp - Project Overview and Technical Documentation**

<br>
<br>

<img width="1399" height="844" alt="image" src="https://github.com/user-attachments/assets/53747aba-a24d-4423-b2e5-7109b8804f59" />




<br>
<br>

<a href="https://doi.org/10.5281/zenodo.19643653"><img src="https://zenodo.org/badge/DOI/10.5281/zenodo.19643653.svg" alt="DOI"></a>

<br>

Version: 1.0

Platform: Windows Desktop

Framework: C# / WPF / .NET

Search Engine: Lucene.NET

Document Type: Project Overview

Distribution: GitHub repository and free ZIP download

**https://doi.org/10.5281/zenodo.19643653**

<br>

**Project Overview and Technical Documentation**

Prepared for repository publication and software distribution.

This document presents the motivation, rationale, design principles, architecture, and technical implementation of DesktopSearchApp. It is intended to accompany the public release of the project on GitHub and to provide users, reviewers, and collaborators with a clear understanding of the application's objectives and internal structure.

<br>

**Introduction**

**DesktopSearchApp** is a Windows desktop application developed to index and search local documents quickly, reliably, and transparently. The application is intended for users who work with large collections of heterogeneous files, including Word documents, PDF files, images, spreadsheets, plain text files, source code, and research-oriented formats.

The application addresses a practical problem frequently encountered in academic, professional, and analytical work: valuable information is often distributed across many folders, formats, and projects, making retrieval slow and inefficient. **DesktopSearchApp** was designed to offer a local-first search solution that allows users to build a searchable index of their files without relying on external cloud services. Unfortunately, the standard Windows Search engine is limited in scope and depth, and often fails to identify existing documents.

The project also serves a broader technical purpose. It demonstrates how file crawling, document extraction, optical character recognition, full-text indexing, diagnostics logging, and incremental file monitoring can be integrated into a desktop application using modern C# and WPF design patterns.

<br>

**Motivation and Rationale**

The primary motivation behind DesktopSearchApp was the need for a practical and extensible desktop search tool capable of handling mixed-format document collections. Existing tools often focus on narrow document categories, provide limited transparency about extraction quality, or rely on system-level indexing approaches that are difficult to inspect or control.

A second reason for developing the application was the specific need to support research and knowledge-intensive workflows. In many academic and professional environments, users work with scanned articles, statistical files, notebooks, code scripts, and supporting documentation simultaneously. **DesktopSearchApp** was therefore designed to accommodate a broader file ecosystem than a standard office-document search application.

A third motivation was software engineering-oriented. The project provides a concrete example of modular desktop application design in which indexing, extraction, search, diagnostics, and monitoring are isolated into distinct services. This makes the application not only useful in practice but also valuable as a demonstrative or portfolio project.

<br>

**Project Objectives**

The project was designed with the following objectives:

• To provide fast full-text search over a local collection of documents.

• To support a wide range of file formats relevant to academic, technical, and professional work.

• To maintain a local-first architecture that preserves privacy and avoids dependence on cloud storage or cloud search services.

• To support OCR-assisted extraction for files that do not contain directly selectable text.

• To include diagnostics and fault-tolerant indexing so that problematic files do not stop the entire indexing process.

• To support incremental updates through folder monitoring and file-change detection.

• To provide a clear, single-window desktop interface for indexing, searching, previewing, and monitoring results.

<br>

**Functional Overview**

DesktopSearchApp enables a user to select a folder, discover supported files in that folder tree, build a Lucene-based index, and perform searches over titles, file names, and extracted document text. Search results are presented together with metadata such as extraction method, OCR usage, extraction status, and preview content.

The application includes a diagnostics panel in the main user interface so that users can observe system activity during indexing and search operations. This panel records informational messages, warnings, and errors, thereby improving transparency and supporting debugging when extraction or indexing issues occur.

The application also includes file-system monitoring. Once a folder is indexed, the system can continue watching for additions, deletions, renames, and modifications, allowing the index to remain synchronized with the underlying file set.

<br>

**Supported File Types**

DesktopSearchApp is designed to support a wide range of file types. These include common office formats, plain text formats, research-oriented files, code files, and presentation documents.

Core Supported Categories

Word processing files: .docx, .doc

PDF files: .pdf

Image files: .jpg, .jpeg, .png

Text and markup files: .txt, .md, .html, .csv, .tex, .cls, .bib

Research and statistical files: .dta, .sav, .rda, .sps, .sas, .sas7bdat, .do, .ado

Code and notebook files: .py, .ipynb, .m, .mat, .r

Spreadsheet files: .xlsx

Presentation files: .pptx, .ppt

Other academic files: .epub

This file coverage reflects the project's focus on real-world research and analytical workflows, where useful information may exist in many different formats rather than in one uniform document class.

<br>

**Technical Architecture**

DesktopSearchApp is built with C#, WPF, and .NET using a service-oriented internal structure. The architecture is modular, with individual services responsible for discrete tasks. This improves maintainability, testability, and extensibility.

<br>

**Core Architectural Components**

FileCrawlerService
Responsible for scanning folders and discovering supported files for indexing.

DocumentExtractionService
Responsible for extracting text and metadata from supported file types.

SearchIndexService
Responsible for building, updating, deleting, and querying the Lucene.NET search index.

DiagnosticsLogService
Responsible for recording informational messages, warnings, and errors for display in the user interface.

FolderWatcherService
Responsible for monitoring indexed folders and detecting file-system changes.

IncrementalIndexService
Responsible for processing file changes and updating the search index incrementally.

PreviewService
Responsible for generating readable preview text for selected search results.

This modular separation allows the application to grow over time without concentrating all logic in the main window or user interface layer.

<br>

**Search Engine Design**
The search engine is implemented using Lucene.NET. Lucene.NET provides full-text indexing and query capabilities suitable for large local collections of documents. It allows the application to search efficiently across extracted content and metadata while retaining control over the index structure.

Each indexed document is stored with fields such as file path, file name, extension, title, extracted document text, OCR usage, extraction method, extraction status, and error-related metadata. This schema enables both retrieval and diagnostics-oriented display.

<br>

The search process supports multiple search scopes:

All searchable fields

Title only

Document text only

The query logic combines exact-term search with prefix-based search so that results can include both highly specific matches and useful partial matches. This improves usability for users who may remember only part of a phrase or title.

<br>

**Indexing Workflow**

The indexing workflow begins with folder selection. Once a folder is chosen, the application scans the folder structure, identifies supported files, and passes them through the extraction and indexing pipeline.

<br>

**Indexing Steps**

The user selects a folder.

The system crawls the directory tree and identifies supported file types.

Each eligible file is processed by the extraction layer.

Extracted text and metadata are converted into a normalized indexed document.

The indexed document is added to the Lucene.NET index.

After completion, folder monitoring may be activated for incremental updates.

A key design principle is fault tolerance. If a single file is malformed, inaccessible, or otherwise problematic, the application is designed to skip the file, log the issue, and continue indexing the remaining files. This is essential for practical use because real-world document repositories often contain inconsistent or damaged content.

<br>

**Document Extraction and OCR**

DesktopSearchApp was intentionally designed to handle more than plain text files. Many documents of practical importance, particularly scanned PDFs and images, require additional processing before they become searchable.

For this reason, the project includes dedicated extraction-related services, including OCR-oriented components. These services make it possible to derive searchable text from non-text-native sources when feasible. The application also records whether OCR was used, which extraction method produced the text, and whether the extraction succeeded or failed.

This information is important because it makes the search corpus more interpretable. Users can distinguish between text extracted directly from a source file and text produced through OCR, and they can identify files that may require reprocessing or manual review.

<br>

**Diagnostics and Reliability**

Diagnostics are a core feature of **DesktopSearchApp** rather than an afterthought. The application includes a diagnostics log that surfaces operational messages directly in the interface.


_Diagnostics Objectives_

• To record application startup and shutdown events.

• To report folder selection and indexing activity.

• To capture warnings about skipped, inaccessible, or malformed files.

• To record search activity and result counts.

• To provide transparency about file-system monitoring events.

A particularly important technical decision in the project was to prevent a single extraction or indexing failure from terminating the full indexing process. Instead, the application logs the problem and continues. This significantly improves robustness when indexing large and diverse collections of files.

<br>

**Incremental Indexing and Folder Monitoring**

Once an index has been built, DesktopSearchApp can continue monitoring the indexed folder. This is achieved through a folder watcher that detects changes in the file system.

_Monitored Events_

• File creation

• File modification

• File deletion

• File rename

Detected changes are passed into the incremental indexing workflow so that the search index remains synchronized with the folder contents. This approach is considerably more efficient than rebuilding the full index every time a document changes.

Incremental indexing is particularly valuable in active project environments, research directories, and work folders where files are updated frequently.

<br>

**User Interface Design**

The user interface is implemented as a WPF desktop window organized around three functional regions: controls for indexing and searching, a results grid, and a combined preview and diagnostics area.

Main Interface Areas

Folder indexing toolbar for selecting a folder and building the index.

Search controls for entering search terms and narrowing the scope.

Results grid for displaying matched files and document metadata.

Preview panel for showing details and extracted snippets from the selected result.

A diagnostics panel for showing operational logs, warnings, and errors.

The design is intentionally practical rather than ornamental. The aim is to support efficient use in desktop environments where clarity, speed, and visibility matter more than visual excess.

<br>

**Intended Use Cases**

DesktopSearchApp is suitable for several user groups and scenarios:

• Academic researchers managing mixed-format document archives.

• Students searching across reading materials, notes, and project files.

• Analysts working with reports, spreadsheets, code, and technical notes.

• Professionals who need a local search tool for project folders and reference libraries.

• Developers and knowledge workers who want a transparent and extensible desktop indexing solution.

The project is especially useful where users prefer local control over their data and want to avoid dependence on cloud indexing systems.

<br>

**Distribution and Repository Use**

The project is intended to be published on GitHub as a source repository. In addition, a ZIP package is provided for free download to make the project easier to distribute and access.

<br>

_Distribution Strategy_

GitHub repository: source code, documentation, and version history.

ZIP package: convenient downloadable copy for users who prefer a packaged version.

<br>

**System Requirements**

Windows 10 or later

Access permissions to folders intended for indexing

<br>

**Basic Usage Workflow**

Launch the application.

Select a folder for indexing.

Build the index.

Search across indexed file content and metadata.

Apply file-type and scope filters if needed.

Select a result to view the preview information.

Double-click a result to open the original file.

<br>

The Lucene index is stored locally in the user profile under application data, ensuring that the indexed content remains on the local machine.
