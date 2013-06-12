// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of "Asim" - A Renewable Energy Power Station
// Control System Simulator
//
// Asim is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PWC.Asim.ExcelTools.Logic
{
    public class Helper
    {
        public const char Quote = '"';
        public const char Delim = ',';
        public const string Squote = "\"";
        public const string Sdelim = ",";

        public static bool IsFalse(string s)
        {
            var test = s.ToLower();
            return (test == "false" || test == "f" ||
                    test == "no" || test == "n" ||
                    test == "0");
        }
    }
}
