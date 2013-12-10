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

namespace PWC.Asim.Core.Contracts
{
    public interface IActor
    {
        /// <summary>
        /// Init() is called once at program start.
        /// </summary>
        void Init();

        /// <summary>
        /// All Read() functions are called at the start of every iteration.
        /// </summary>
        /// <param name="iteration"></param>
        void Read(ulong iteration);

        /// <summary>
        /// All Run() functions are called after all Read() functions have been completed.
        /// </summary>
        /// <param name="iteration"></param>
        void Run(ulong iteration);

        /// <summary>
        /// All write functions are called after all Run() functions have been completed.
        /// </summary>
        /// <param name="iteration"></param>
        void Write(ulong iteration);

        /// <summary>
        /// Finish() is called once at program end.
        /// </summary>
        void Finish();
    }
}
