// Copyright (C) 2012, 2013  Power Water Corporation
//
// This file is part of the Solar Load Model - A Renewable Energy Power Station
// Control System Simulator
//
// The Solar Load Model is free software: you can redistribute it and/or modify
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SolarLoadModel.Utils
{
    class DelayedExecution
    {
        public ulong ItToRun { get; set; }
        public Action Callback { get; set; }

        public DelayedExecution(ulong itToRun, Action callback)
        {
            ItToRun = itToRun;
            Callback = callback;
        }
    }

    public class ExecutionManager
    {
        private readonly List<DelayedExecution> _todo = new List<DelayedExecution>();
        private ulong _thisIter = 0;

        public void After(ulong t, Action a)
        {
            if (t == 0)
                a();
            else
                _todo.Add(new DelayedExecution(_thisIter + t, a));
        }

        // todo: opportunity to speed this up as the number of actions gets larger, _todo is not sorted or searched well
        public void RunActions(ulong iter)
        {
            _thisIter = iter;
            for (int i = _todo.Count - 1; i >= 0; i--)
            {
                var todo = _todo.ElementAt(i);
                if (todo.ItToRun == iter)
                {
                    todo.Callback();
                    _todo.RemoveAt(i);
                }
            }
        }
    }
}
