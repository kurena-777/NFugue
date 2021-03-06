﻿using NFugue.Extensions;
using NFugue.Parsing;
using NFugue.Patterns;
using NFugue.Providers;
using NFugue.Theory;
using System;
using System.Text;

namespace NFugue.Staccato.Subparsers
{
    public class SignatureSubparser : ISubparser, IKeyProvider
    {
        public static readonly string KeySignatureString = "KEY:";
        public static readonly string TimeSignatureString = "TIME:";
        public static readonly string SeparatorString = "/";

        // Major and Minor Key Signatures
        // For the Major Key Signatures, 'C' is at the center position. Key signatures defined with flats are to the left of C; key signatures defined with sharps are to the right of C
        // For the Minor Key Signatures, 'A' is at the center position. Key signatures defined with flats are to the left of A; key signatures defined with sharps are to the right of A
        public static readonly string[] MajorKeySignatures = { "Cb", "Gb", "Db", "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#" };
        public static readonly string[] MinorKeySignatures = { "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#", "G#", "D#", "A#" };
        public static readonly int KeySigMidpoint = 7;

        public static readonly string MajorAbbreviation = "maj";
        public static readonly string MinorAbbreviation = "min";

        public static readonly char SharpChar = '#';
        public static readonly char FlatChar = 'B';

        public bool Matches(string music)
        {
            return MatchesKeySignature(music) || MatchesTimeSignature(music);
        }

        public bool MatchesKeySignature(string music)
        {
            return music.Length >= KeySignatureString.Length &&
                   music.Substring(0, KeySignatureString.Length) == KeySignatureString;
        }

        public bool MatchesTimeSignature(string music)
        {
            return music.Length >= TimeSignatureString.Length &&
               music.Substring(0, TimeSignatureString.Length) == TimeSignatureString;
        }

        public TokenType GetTokenType(string tokenString)
        {
            if (MatchesKeySignature(tokenString))
            {
                return TokenType.KeySignature;
            }
            if (MatchesTimeSignature(tokenString))
            {
                return TokenType.TimeSignature;
            }
            return TokenType.UnknownToken;
        }

        public int Parse(string music, StaccatoParserContext context)
        {
            if (MatchesKeySignature(music))
            {
                int posNextSpace = music.FindNextOrEnd(' ');
                Key key = CreateKey(music.Substring(KeySignatureString.Length, posNextSpace - KeySignatureString.Length));
                context.Key = key;
                context.Parser.OnKeySignatureParsed(key.Root.PositionInOctave, (int)key.Scale.Type);
                return posNextSpace + 1;
            }
            if (MatchesTimeSignature(music))
            {
                int posNextSpace = music.FindNextOrEnd(' ');
                string timeString = music.Substring(TimeSignatureString.Length,
                    posNextSpace - TimeSignatureString.Length);
                int posOfSlash = timeString.IndexOf(SeparatorString, StringComparison.Ordinal);
                if (posOfSlash == -1)
                {
                    throw new ParserException(StaccatoMessages.NoTimeSignatureSeparator + timeString);
                }
                int numerator = int.Parse(timeString.Substring(0, posOfSlash));
                int denominator = int.Parse(timeString.Substring(posOfSlash + 1, timeString.Length - posOfSlash - 1));
                var timeSignature = new TimeSignature(numerator, denominator);
                context.TimeSignature = timeSignature;
                context.Parser.OnTimeSignatureParsed(numerator, denominator);
                return posNextSpace + 1;
            }
            return 0;
        }

        public Key CreateKey(string keySignature)
        {
            // If the key signature starts with K, it is expected to contain a set of flat or sharp characters equal to the number of flats
            // or sharps one would see on the staff for the corresponding key. Defaults to MAJOR key.
            if (keySignature[0] == 'K' && (keySignature.IndexOf(SharpChar) == 1 || (keySignature.ToUpper().IndexOf(FlatChar) == 1)))
            {
                return CreateKeyFromAccidentals(keySignature);
            }
            // Otherwise, pass the string value - something like "Cmaj" - to createChord and generate a Key from the intervals in that chord
            return new Key(ChordProviderFactory.GetChordProvider().CreateChord(keySignature));
        }

        public string CreateKeyString(int notePositionInOctave, int scale)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Note.NoteNamesCommon[notePositionInOctave]);
            if (scale == (int)ScaleType.Major)
            {
                sb.Append(MajorAbbreviation);
            }
            else
            {
                sb.Append(MinorAbbreviation);
            }
            return sb.ToString();
        }

        public int ConvertAccidentalCountToKeyRootPositionInOctave(int accidentalCount, int scale)
        {
            if (scale == (int)ScaleType.Major)
            {
                return new Note(MajorKeySignatures[KeySigMidpoint - accidentalCount]).PositionInOctave;
            }
            return new NFugue.Theory.Note(MinorKeySignatures[KeySigMidpoint - accidentalCount]).PositionInOctave;
        }

        public int ConvertKeyToInt(Key key)
        {
            string noteName = Note.DispositionedToneStringWithoutOctave(key.Scale.Disposition, key.Root.Value);
            if (noteName == null)
            {
                return 0;
            }
            for (int b = -KeySigMidpoint; b < KeySigMidpoint + 1; b++)
            {
                if (Note.IsSameNote(noteName, key.Scale.Equals(Scale.Major) ? MajorKeySignatures[KeySigMidpoint + b] : MinorAbbreviation[KeySigMidpoint + b].ToString()))
                {
                    return (b * key.Scale.Disposition);
                }
            }
            return 0;
        }

        private Key CreateKeyFromAccidentals(string keySignature)
        {
            return new Key(MajorKeySignatures[KeySigMidpoint + CountAccidentals(keySignature)] + MajorAbbreviation);
        }

        private static int CountAccidentals(string keySignatureAsFlatsOrSharps)
        {
            int keySig = 0;
            foreach (char ch in keySignatureAsFlatsOrSharps.ToUpper())
            {
                if (ch == FlatChar) keySig--;
                if (ch == SharpChar) keySig++;
            }
            return keySig;
        }
    }
}