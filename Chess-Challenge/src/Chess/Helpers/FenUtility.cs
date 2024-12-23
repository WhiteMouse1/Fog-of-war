﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChessChallenge.Chess
{
    // Helper class for dealing with FEN strings
    public static class FenUtility
    {
        public const string StartPositionFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        // Load position from fen string
        public static PositionInfo PositionFromFen(string fen)
        {

            PositionInfo loadedPositionInfo = new(fen);
            return loadedPositionInfo;
        }

        /// <summary>
        /// Get the fen string of the current position
        /// When alwaysIncludeEPSquare is true the en passant square will be included
        /// in the fen string even if no enemy pawn is in a position to capture it.
        /// </summary>
        public static string CurrentFen(Board board, bool alwaysIncludeEPSquare = true)
        {
            string fen = "";
            for (int rank = 7; rank >= 0; rank--)
            {
                int numEmptyFiles = 0;
                for (int file = 0; file < 8; file++)
                {
                    int i = rank * 8 + file;
                    int piece = board.Square[i];
                    if (piece != 0)
                    {
                        if (numEmptyFiles != 0)
                        {
                            fen += numEmptyFiles;
                            numEmptyFiles = 0;
                        }
                        bool isBlack = PieceHelper.IsColour(piece, PieceHelper.Black);
                        int pieceType = PieceHelper.PieceType(piece);
                        char pieceChar = ' ';
                        switch (pieceType)
                        {
                            case PieceHelper.Rook:
                                pieceChar = 'R';
                                break;
                            case PieceHelper.Knight:
                                pieceChar = 'N';
                                break;
                            case PieceHelper.Bishop:
                                pieceChar = 'B';
                                break;
                            case PieceHelper.Queen:
                                pieceChar = 'Q';
                                break;
                            case PieceHelper.King:
                                pieceChar = 'K';
                                break;
                            case PieceHelper.Pawn:
                                pieceChar = 'P';
                                break;
                        }
                        fen += isBlack ? pieceChar.ToString().ToLower() : pieceChar.ToString();
                    }
                    else
                    {
                        numEmptyFiles++;
                    }

                }
                if (numEmptyFiles != 0)
                {
                    fen += numEmptyFiles;
                }
                if (rank != 0)
                {
                    fen += '/';
                }
            }


            // Side to move
            fen += ' ';
            fen += (board.IsWhiteToMove) ? 'w' : 'b';

            // Castling
            fen += ' ';
            fen += board.WhiteKingside ? "K" : "";
            fen += board.WhiteQueenside ? "Q" : "";
            fen += board.BlackKingside ? "k" : "";
            fen += board.BlackQueenside ? "q" : "";
            fen += ((board.currentGameState.castlingRights) == 0) ? "-" : "";

            // En-passant
            fen += ' ';
            int epFileIndex = board.currentGameState.enPassantFile - 1;
            int epRankIndex = (board.IsWhiteToMove) ? 5 : 2;

            bool isEnPassant = epFileIndex != -1;
            bool includeEP = alwaysIncludeEPSquare || EnPassantCanBeCaptured(epFileIndex, epRankIndex, board);
            if (isEnPassant && includeEP)
            {
                fen += BoardHelper.SquareNameFromCoordinate(epFileIndex, epRankIndex);
            }
            else
            {
                fen += '-';
            }

            // 50 move counter
            fen += ' ';
            fen += board.currentGameState.fiftyMoveCounter;

            // Full-move count (should be one at start, and increase after each move by black)
            fen += ' ';
            fen += (board.plyCount / 2) + 1;

            return fen;
        }

        static bool EnPassantCanBeCaptured(int epFileIndex, int epRankIndex, Board board)
        {
            Coord captureFromA = new Coord(epFileIndex - 1, epRankIndex + (board.IsWhiteToMove ? -1 : 1));
            Coord captureFromB = new Coord(epFileIndex + 1, epRankIndex + (board.IsWhiteToMove ? -1 : 1));
            int epCaptureSquare = new Coord(epFileIndex, epRankIndex).SquareIndex;
            int friendlyPawn = PieceHelper.MakePiece(PieceHelper.Pawn, board.MoveColour);

            return CanCapture(captureFromA) || CanCapture(captureFromB);

            bool CanCapture(Coord from)
            {
                bool isPawnOnSquare = board.Square[from.SquareIndex] == friendlyPawn;
                return from.IsValidSquare() && isPawnOnSquare;
            }
        }

        private static int[] GetAvailableSquares(Board board){
            Move[] moves = new MoveGenerator().GenerateFowMoves(board).ToArray();
            List<int> availableSquares = new List<int>();
            foreach (Move move in moves)
            {
                availableSquares.Add(move.StartSquareIndex);
                availableSquares.Add(move.TargetSquareIndex);
            }
            foreach (PieceList pl in board.pieceLists)
            {
                if (pl != null && pl.Count != 0 && PieceHelper.IsColour(board.Square[pl.occupiedSquares[0]], 
                board.IsWhiteToMove ? PieceHelper.White : PieceHelper.Black))
                {
                    for (int i = 0; i < pl.Count; i++)
                    {
                        availableSquares.Add(pl.occupiedSquares[i]);
                    }
                }
            }
            return availableSquares.ToArray();
        }

        public static string CurrentFoWFen(Board board, bool alwaysIncludeEPSquare = true)
        {
            int[] sqs = GetAvailableSquares(board);
            string fen = "";
            for (int rank = 7; rank >= 0; rank--)
            {
                int numEmptyFiles = 0;
                for (int file = 0; file < 8; file++)
                {
                    int i = rank * 8 + file; // Linear index for the square.
                    int piece = board.Square[i];
                    bool isFogged = !sqs.Contains(i);

                    if (isFogged)
                    {
                        // Square is under fog; reset numEmptyFiles and add '?' to the FEN.
                        if (numEmptyFiles != 0)
                        {
                            fen += numEmptyFiles;
                            numEmptyFiles = 0;
                        }
                        fen += "?";
                    }
                    else if (piece != 0)
                    {
                        // Square contains a piece; reset numEmptyFiles and add the piece char.
                        if (numEmptyFiles != 0)
                        {
                            fen += numEmptyFiles;
                            numEmptyFiles = 0;
                        }
                        bool isBlack = PieceHelper.IsColour(piece, PieceHelper.Black);
                        int pieceType = PieceHelper.PieceType(piece);
                        char pieceChar = pieceType switch
                        {
                            PieceHelper.Rook => 'R',
                            PieceHelper.Knight => 'N',
                            PieceHelper.Bishop => 'B',
                            PieceHelper.Queen => 'Q',
                            PieceHelper.King => 'K',
                            PieceHelper.Pawn => 'P',
                            _ => ' ' // Default case for invalid piece type.
                        };
                        fen += isBlack ? char.ToLower(pieceChar) : pieceChar;
                    }
                    else
                    {
                        // Square is empty; increment the empty square counter.
                        numEmptyFiles++;
                    }
                }

                // Append remaining empty squares for the rank.
                if (numEmptyFiles != 0)
                {
                    fen += numEmptyFiles;
                }

                // Add rank separator unless it's the last rank.
                if (rank != 0)
                {
                    fen += '/';
                }
            }

            return fen;
        }

        
        public readonly struct PositionInfo
        {
            public readonly string fen;
            public readonly ReadOnlyCollection<int> squares;

            // Castling rights
            public readonly bool whiteCastleKingside;
            public readonly bool whiteCastleQueenside;
            public readonly bool blackCastleKingside;
            public readonly bool blackCastleQueenside;
            // En passant file (1 is a-file, 8 is h-file, 0 means none)
            public readonly int epFile;
            public readonly bool whiteToMove;
            // Number of half-moves since last capture or pawn advance
            // (starts at 0 and increments after each player's move)
            public readonly int fiftyMovePlyCount;
            // Total number of moves played in the game
            // (starts at 1 and increments after black's move)
            public readonly int moveCount;

            public PositionInfo(string fen)
            {
                this.fen = fen;
                int[] squarePieces = new int[64];

                string[] sections = fen.Split(' ');

                int file = 0;
                int rank = 7;

                foreach (char symbol in sections[0])
                {
                    if (symbol == '/')
                    {
                        file = 0;
                        rank--;
                    }
                    else
                    {
                        if (char.IsDigit(symbol))
                        {
                            file += (int)char.GetNumericValue(symbol);
                        }
                        else
                        {
                            int pieceColour = (char.IsUpper(symbol)) ? PieceHelper.White : PieceHelper.Black;
                            int pieceType = char.ToLower(symbol) switch
                            {
                                'k' => PieceHelper.King,
                                'p' => PieceHelper.Pawn,
                                'n' => PieceHelper.Knight,
                                'b' => PieceHelper.Bishop,
                                'r' => PieceHelper.Rook,
                                'q' => PieceHelper.Queen,
                                _ => PieceHelper.None
                            };

                            squarePieces[rank * 8 + file] = pieceType | pieceColour;
                            file++;
                        }
                    }
                }

                squares = new(squarePieces);

                whiteToMove = (sections[1] == "w");

                string castlingRights = sections[2];
                whiteCastleKingside = castlingRights.Contains('K');
                whiteCastleQueenside = castlingRights.Contains('Q');
                blackCastleKingside = castlingRights.Contains('k');
                blackCastleQueenside = castlingRights.Contains('q');

                // Default values
                epFile = 0;
                fiftyMovePlyCount = 0;
                moveCount = 0;

                if (sections.Length > 3)
                {
                    string enPassantFileName = sections[3][0].ToString();
                    if (BoardHelper.fileNames.Contains(enPassantFileName))
                    {
                        epFile = BoardHelper.fileNames.IndexOf(enPassantFileName) + 1;
                    }
                }

                // Half-move clock
                if (sections.Length > 4)
                {
                    int.TryParse(sections[4], out fiftyMovePlyCount);
                }
                // Full move number
                if (sections.Length > 5)
                {
                    int.TryParse(sections[5], out moveCount);
                }
            }
        }
    }
}