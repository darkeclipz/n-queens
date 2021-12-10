using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NQueens
{
    class Program
    {
        static void Main()
        {
            ShowWindow(ThisConsole, MAXIMIZE);
            CspModel model = new();
            int n = 128;
            for (int i = 0; i < n; i++)
            {
                model.CreateVariable($"Q{i}", i);
            }
            MinConflictSolver solver = new(model);
            solver.Solve();
        }

        #region Windows API shit
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        private static IntPtr ThisConsole = GetConsoleWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int HIDE = 0;
        private const int MAXIMIZE = 3;
        private const int MINIMIZE = 6;
        private const int RESTORE = 9;
        #endregion
    }

    class Variable
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public bool IsSet { get; set; }
        public int Index { get; set; }

        public Variable(string name, int index)
        {
            Name = name;
            Index = index;
        }

        public void Assign(int value)
        {
            Value = value;
            IsSet = true;
        }

        public void Unassign()
        {
            Value = 0;
            IsSet = false;
        }

        public override string ToString()
        {
            return $"{Name} = {Value} (IsSet: {IsSet})";
        }
    }

    class QueenConstraint
    {
        public static bool Satisfied(int i, int j, int Qi, int Qj)
        {
            // |i-j|!=|Qi-Qj| where Qi != Qj
            return Qi != Qj && Math.Abs(i - j) != Math.Abs(Qi - Qj);
        }
    }
    class CspModel
    {
        public List<Variable> Variables { get; set; } = new();

        public Variable CreateVariable(string name, int index)
        {
            Variable variable = new(name, index);
            Variables.Add(variable);
            return variable;
        }
    }

    class MinConflictSolver
    {
        private CspModel Model { get; }
        private Random Rng { get; } = new();
        private int TotalSteps { get; set; }

        public MinConflictSolver(CspModel model)
        {
            Model = model;
        }

        public void Solve()
        {
            bool solved = false;
            int attempts = 0;
            Stopwatch stopwatch = new();
            stopwatch.Start();

            while(!solved)
            {
                GenerateRandomSolution(); // The algorithm works best if we have a reasonable starting point.
                solved = MinConflict();
                attempts++;
            }

            stopwatch.Stop();

            BoardPrinter.Print(Model);
            Console.WriteLine($"Total steps: {TotalSteps}");
            Console.WriteLine($"Total attempts: {attempts}");
            Console.WriteLine($"Total runtime is {stopwatch.ElapsedMilliseconds} milliseconds.");
        }

        private bool MinConflict()
        {
            int n = Model.Variables.Count;
            int maxSteps = n * n;
            TotalSteps = 0;

            while (maxSteps-- > 0)
            {
                Variable variable = GetMostConflictedVariable();
                if(variable == null)
                {
                    break;
                }

                int leastConflictedPosition = GetLeastConflictedPosition(variable);
                variable.Assign(leastConflictedPosition);
                TotalSteps++;
            }

            if (maxSteps <= 0)
            {
                return false;
            }

            return true;
        }

        private Variable GetRandomlyConflictedVariable()
        {
            List<Variable> conflictedVariable = new();
            for(int i = 0; i < Model.Variables.Count; i++)
            {
                Variable variable = Model.Variables[i];
                if(CountConflicts(variable) > 0)
                {
                    conflictedVariable.Add(variable);
                }
            }

            Variable randomVariable = conflictedVariable[Rng.Next(0, conflictedVariable.Count)];
            return randomVariable;
        }

        private Variable GetMostConflictedVariable()
        {
            List<Variable> mostConflictedVariables = new();
            int countMostConflicted = 0;

            for (int i = 0; i < Model.Variables.Count; i++)
            {
                Variable variable = Model.Variables[i];
                int numOfConflicts = CountConflicts(variable);
                if (numOfConflicts >= countMostConflicted)
                {
                    if(numOfConflicts > countMostConflicted)
                    {
                        mostConflictedVariables.Clear();
                        countMostConflicted = Math.Max(numOfConflicts, countMostConflicted);
                    }

                    mostConflictedVariables.Add(variable);
                }
            }

            if(countMostConflicted == 0)
            {
                return null;
            }

            return mostConflictedVariables[Rng.Next(0, mostConflictedVariables.Count)];
        }

        private int GetLeastConflictedPosition(Variable variable)
        {
            Dictionary<int, int> count = new();
            int i = variable.Index;
            int n = Model.Variables.Count;
            int minValue = int.MaxValue;
            for(int j = 0; j < n; j++)
            {
                if(i == j)
                {
                    continue;
                }

                int numOfConflicts = 0;
                for(int k = 0; k < n; k++)
                {
                    if (j != k && !QueenConstraint.Satisfied(j, k, j, Model.Variables[k].Value))
                    {
                        numOfConflicts++;
                    }
                }

                minValue = Math.Min(minValue, numOfConflicts);
                count.Add(j, numOfConflicts);
            }

            List<int> positions = new();
            foreach(var (p, c) in count)
            {
                if(c == minValue)
                {
                    positions.Add(p);
                }
            }

            return positions[Rng.Next(0, positions.Count)];
        }

        private void GenerateRandomSolution()
        {
            Random random = new();
            int n = Model.Variables.Count;

            foreach (Variable variable in Model.Variables)
            {
                int value = random.Next(0, n);
                variable.Assign(value);
            }
        }

        public int CountConflicts(Variable variable)
        {
            int n = Model.Variables.Count;
            int i = variable.Index;
            int count = 0;
            for (int j = 0; j < n; j++)
            {
                if (i != j && !QueenConstraint.Satisfied(i, j, variable.Value, Model.Variables[j].Value))
                {
                    count++;
                }
            }
            return count;
        }
    }

    static class BoardPrinter
    {
        public static void Print(CspModel model, int highlightVariable = -1)
        {
            StringBuilder sb = new ();
            MinConflictSolver solver = new(model);

            for(int i = 0; i < model.Variables.Count; i++)
            {
                Variable variable = model.Variables[i];
                for(int j = 0; j < model.Variables.Count; j++)
                {
                    if(variable.Value == j)
                    {
                        if(highlightVariable >= 0 && variable.Index == highlightVariable)
                        {
                            sb.Append('X');
                        }
                        else
                        {
                            sb.Append('Q');
                        }
                    }
                    else
                    {
                        sb.Append('.');
                    }

                    sb.Append(' ');
                }

                int numOfConflicts = solver.CountConflicts(variable);
                sb.Append($"  ({numOfConflicts} conflicts)");

                sb.AppendLine();
            }

            Console.WriteLine(sb);
        }
    }
}