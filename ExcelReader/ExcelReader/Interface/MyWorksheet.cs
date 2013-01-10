// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of Excel Reader - An Excel Manipulation Program
//
// Excel Reader is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Foobar is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExcelReader.Interface
{
    public class MyWorksheet
    {
        public string Name { get; set; }
        public object[,] Data { get; set; }
    }
}
