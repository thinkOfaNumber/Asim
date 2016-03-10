This is a test framework for dropping folders of tests (one test per
folder) and referencing the results over time.

** This is designed to be run by bash.  This is available on Windows
   by installing Cygwin from cygwin.com

ADDING YOUR TEST

1. Copy your test into a folder tests/mytest, as described in NEW
   TESTS below.
2. Remove any Template directives from the Excel config file.
3. Make all paths relative
3. Run the test once, ensuring the output goes into the same folder
4. Remove any dates from the output files

You're now ready to test! Run the test script from the DropTests
folder like so:
       $ ./run-tests.sh



NEW TESTS

- Each test must go into the folder "tests"
- Each test must be in its own folder.
- The suggested folder name is <number>-<testname> eg 00-PVtest,
- 01-loadtest, etc.
- Each test must contain the file "run", the first line of which will
  contain the name of the excel spreadsheet containing the simulator
  configuration. No paths are allowed, eg it must be in the root of
  the test folder.  Subsequent lines will be ignored.
- The config spreadsheet must reference the relative path "bin" for
  the ExcelReader and SolarLoadModel binaries.

BINARIES

- The binaries to be tested must go into the folder "bin".  Only
  "ExcelReader.exe" and "SolarLoadModel.exe" will be expected in this
  directory.
- These binaries will be copied into each test folder in the directory
  "bin" and run as if they were started by the manual excel macro.

RESULTS

- tests will be copied into the folder "results", which will be wiped
  at the beginning of each group of tests
- hence detailed results will be available after a test in the results
  folder for manual inspection
- stdout will print a running list of each test with a pass/fail, and
  a total number of passed / failed tests

METHOD

Each folder in "tests" will be copied in turn into "results", and have
the "bin" folder applied into it.  The ExcelReader and SolarLoadModel
will be run accordingly.  All CSV files (and ONLY CSV files) will be
diffed against the original test folder in the "tests" folder.  IFF
all CSV files are identical, the test passes.  Otherwise the test
fails.

USAGE

$ ./run-tests.pl
