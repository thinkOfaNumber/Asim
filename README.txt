These files constitute the project "Asim".

Copyright (C) 2012, 2013  Power Water Corporation

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.


USING

Further details on running the program can be found in the manual -
"Reference Manaul.pdf", in the docs folder.


CONTRIBUTING

Please use the Fork & Pull method to submit a pull request via GitHub.

All pull requests must include:
 - Formatted and tested code
 - Updated documentation
 - Unit tests to thoroughly test the behaviour and edge cases of your feature / bugfix


BUG REPORTS

Due to the large size of output files, please make bug reports as small as possible.
If possible, reproduce the bug using a small set of input and output files representing
a few hours of simulation time.

Include in your bug report all input files and executable files required to
reproduce the bug, and detailed steps to reproduce the behaviour, as well as
what behaviour you were expecing.


KNOWN ISSUES

- various input values are not sanity checked - such as generator sizes, fuel curves, etc.
so it is possible to get strange results by supplying incorrect data such as a negative
generator rating.

- all files are written to on the very first iteration.  This means that there will be a slight
statistical error (of one second) on any output files with an output frequency greater than
one.

- if the output file frequency does not divide equally by the number of iterations, the
output file will not get written to on the last iteration.
