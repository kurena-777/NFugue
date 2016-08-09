﻿using NFugue.Extensions;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NFugue.Theory
{
    public class Intervals
    {
        private static readonly IDictionary<int, int> wholeNumberDegreeToHalfsteps;
        private static readonly IDictionary<int, int> halfstepsToWholeNumberDegree;

        private static string[] CandidateIntervals =
        {
            "b1", "1", "#1", "b2", "2", "#2", "b3", "3", "#3",
            "b4", "4", "#4", "b5", "5", "#5", "b6", "6", "#6",
            "b7", "7", "#7", "b8", "8", "#8", "b9", "9", "#9",
            "b10", "10", "#10", "b11", "11", "#11", "b12", "12", "#12",
            "b13", "13", "#13", "b14", "14", "#14", "b15", "15", "#15"
        };

        static Intervals()
        {
            wholeNumberDegreeToHalfsteps = new Dictionary<int, int>
            {
                {1, 0},
                {2, 2},
                {3, 4},
                {4, 5},
                {5, 7},
                {6, 9},
                {7, 11},
                {8, 12},
                {9, 14},
                {10, 16},
                {11, 17},
                {12, 19},
                {13, 21},
                {14, 23},
                {15, 24}
            };

            halfstepsToWholeNumberDegree = wholeNumberDegreeToHalfsteps.ReverseDictionary();
        }

        private string intervalPattern;
        private static readonly Regex numberPattern = new Regex("\\d+");

        public Intervals(string intervalPattern)
        {
            this.intervalPattern = intervalPattern;
        }

        public string AsSequence { get; set; }
        public Note Root { get; set; }

        public Intervals SetRoot(string root)
        {
            Root = new NoteProvider().CreateNote(root);
            return this;
        }

        public Pattern GetPattern()
        {
            string[] intervals = intervalPattern.Split(' ');
            int counter = 0;
            Note[] candidateNotes = new Note[intervals.Length];

            foreach (var interval in intervals)
            {
                Note note = new Note((byte)(Root.Value + GetHalfsteps(interval)));
                candidateNotes[counter++] = note;
            }
            Pattern intervalNotes = new Pattern(candidateNotes);

            //if (asSequence != null)
            {
                //return ReplacementFormatUtil.replaceDollarsWithCandidates(asSequence, candidateNotes, intervalNotes);
            }
            // else
            {
                return intervalNotes;
            }
        }

        public IList<Note> GetNotes()
        {
            List<Note> noteList = new List<Note>();
            Pattern pattern = GetPattern();
            foreach (var split in pattern.ToString().Split(' '))
            {
                if (new NoteSubparser().Matches(split))
                {
                    noteList.Add(new Note(split));
                }
            }
            return noteList;
        }

        //TODO: indexer?
        public string GetNthInterval(int n)
        {
            return intervalPattern.Split(' ')[n];
        }

        public int Size => intervalPattern.Split(' ').Length;

        public static int GetHalfsteps(string wholeNumberDegree)
        {
            return wholeNumberDegreeToHalfsteps[NumberPortionOfInterval(wholeNumberDegree)] +
                   CalculateHalfstepDeltaFromFlatsAndSharps(wholeNumberDegree);
        }

        public int[] ToHalfstepArray()
        {
            string[] intervals = intervalPattern.Split(' ');
            int[] halfSteps = new int[intervals.Length];
            for (int i = 0; i < intervals.Length; i++)
            {
                halfSteps[i] = GetHalfsteps(intervals[i]);
            }
            return halfSteps;
        }

        private static int CalculateHalfstepDeltaFromFlatsAndSharps(string wholeNumberDegree)
        {
            int numHalfsteps = 0;
            foreach (char ch in wholeNumberDegree.ToUpper())
            {
                if (ch == 'B')
                {
                    numHalfsteps -= 1;
                }
                else if (ch == '#')
                {
                    numHalfsteps += 1;
                }
            }
            return numHalfsteps;
        }

        private static int NumberPortionOfInterval(string interval)
        {
            var match = numberPattern.Match(interval);
            if (match.Success)
            {
                return int.Parse(match.Groups[0].ToString());
            }
            return 0;
        }

        public Intervals Rotate(int n)
        {
            string[] intervals = intervalPattern.Split(' ');
            n %= intervals.Length;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < intervals.Length - n; i++)
            {
                sb.Append(intervals[n + i]);
                sb.Append(" ");
            }
            for (int i = 0; i < n; i++)
            {
                sb.Append(intervals[i]);
                sb.Append(" ");
            }
            intervalPattern = sb.ToString().Trim();
            return this;
        }

        public override string ToString()
        {
            return intervalPattern;
        }

        public static Intervals CreateIntervalsFromNotes(Pattern pattern)
        {
            return CreateIntervalsFromNotes(pattern.ToString());
        }

        public static Intervals CreateIntervalsFromNotes(string noteString)
        {
            string[] noteStrings = noteString.Split(' ');
            Note[] notes = new Note[noteStrings.Length];
            for (int i = 0; i < noteStrings.Length; i++)
            {
                notes[i] = new NoteProvider().CreateNote(noteStrings[i]);
            }
            return CreateIntervalsFromNotes(notes);
        }

        public static Intervals CreateIntervalsFromNotes(Note[] notes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("1 ");
            for (int i = 1; i < notes.Length; i++)
            {
                int diff = 0;
                if (notes[i].PositionInOctave() < notes[0].PositionInOctave())
                {
                    diff = notes[i].PositionInOctave() + 12 - notes[0].PositionInOctave();
                }
                else
                {
                    diff = notes[i].PositionInOctave() - notes[0].PositionInOctave();
                }
                if (!halfstepsToWholeNumberDegree.ContainsKey(diff))
                {
                    diff += 1;
                    sb.Append("b");
                }
                int wholeNumberDegree = halfstepsToWholeNumberDegree[diff];
                sb.Append(wholeNumberDegree);
                sb.Append(" ");
            }
            return new Intervals(sb.ToString().Trim());
        }

        #region Equality members
        protected bool Equals(Intervals other)
        {
            return string.Equals(intervalPattern, other.intervalPattern);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Intervals)obj);
        }

        public override int GetHashCode()
        {
            return intervalPattern?.GetHashCode() ?? 0;
        }
        #endregion
    }
}