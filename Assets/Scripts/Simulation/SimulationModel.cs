﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This is the god class that controls everything
public class SimulationModel : MonoBehaviour
{
    // Simulation
    Population Population;
    public SimulationPhase SimulationPhase;
    public int MatchesPlayed;

    private int PopulationSize = 600;
    private int MatchesPerGeneration = 12;

    // Matches
    public Match MatchModel;
    public List<Match> Matches;

    // Match rules
    private int StartHealth = 30;
    private int StartCardOptions = 1;
    private int MinCardOptions = 1;
    private int MaxCardOptions = 5;
    private int MaxMinions = 50;
    private int MaxMinionsPerType = 8;
    private int FatigueDamageStartTurn = 20;

    // UI
    public SimulationUI SimulationUI;
    public MatchUI MatchUI;
    private int VisualBoardHeight = 8;

    // Visuals
    public VisualPlayer VisualPlayer;
    public VisualMinion VisualMinion;
    public Match VisualMatch;

    // Card Statistics
    public Dictionary<int, int> CardsPicked;
    public Dictionary<int, int> CardsPickedByWinner;
    public Dictionary<int, int> CardsPickedByLoser;
    public Dictionary<int, int> CardsNotPicked;

    public Dictionary<int, float> CardPickedPerMatch;
    public Dictionary<int, float> CardPickrate;
    public Dictionary<int, float> CardWinrate;

    // Match Statistics
    public int Player1WonMatches;
    public int TotalMatches;
    public int TotalTurns;
    public int WinnerNumCardOptions;
    public int LoserNumCardOptions;

    // Start is called before the first frame update
    void Start()
    {
        // Init cards
        CardList.InitCardList();

        // Init population
        int inputSize = 16;
        int outputSize = CardList.Cards.Count;
        int hiddenSize = (inputSize + outputSize) / 2;
        Population = new Population(PopulationSize, inputSize, new int[] { /*hiddenSize*/ }, outputSize);
        //EvolutionInformation info = Population.EvolveGeneration(7);
        //SimulationUI.EvoStats.UpdateStatistics(info);
        SimulationUI.SpeciesScoreboard.UpdateScoreboard(Population);

        // Generate matches
        Matches = new List<Match>();
        GenerateMatches();

        SimulationPhase = SimulationPhase.MatchesReady;
        MatchesPlayed = 0;

        // Init statistics
        ResetStatistics();

        // UI
        SimulationUI.MatchRules.UpdateStatistics(StartHealth, StartCardOptions, MinCardOptions, MaxCardOptions, FatigueDamageStartTurn, MaxMinions, MaxMinionsPerType);
    }

    private void GenerateMatches()
    {
        Matches.Clear();
        List<Subject> remainingSubjects = new List<Subject>();
        remainingSubjects.AddRange(Population.Subjects);

        // If last round of generation, let the best two subjects play against each other and watch the game
        if (SimulationUI.WatchGame.isOn && MatchesPlayed == MatchesPerGeneration - 1)
        {
            List<Subject> bestSubjects = remainingSubjects.OrderByDescending(x => x.Wins).Take(2).ToList();
            Subject sub1 = bestSubjects[0];
            remainingSubjects.Remove(sub1);
            Subject sub2 = bestSubjects[1];
            remainingSubjects.Remove(sub2);
            sub1.Opponents.Add(sub2);
            sub2.Opponents.Add(sub1);

            Match match = new Match();

            Player player1 = new AIPlayer(match, sub1);
            Player player2 = new AIPlayer(match, sub2);
            match.InitGame(player1, player2, StartHealth, StartCardOptions, MinCardOptions, MaxCardOptions, MaxMinions, MaxMinionsPerType, FatigueDamageStartTurn, true, false);
            Matches.Add(match);
        }

        // Generate random matches
        while(remainingSubjects.Count > 0)
        {
            Subject sub1 = remainingSubjects[Random.Range(0, remainingSubjects.Count)];
            remainingSubjects.Remove(sub1);
            Subject sub2 = remainingSubjects[Random.Range(0, remainingSubjects.Count)];
            remainingSubjects.Remove(sub2);
            sub1.Opponents.Add(sub2);
            sub2.Opponents.Add(sub1);

            Match match = new Match();

            Player player1 = new AIPlayer(match, sub1);
            Player player2 = new AIPlayer(match, sub2);
            match.InitGame(player1, player2, StartHealth, StartCardOptions, MinCardOptions, MaxCardOptions, MaxMinions, MaxMinionsPerType, FatigueDamageStartTurn, false, false);
            Matches.Add(match);
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (SimulationPhase)
        {
            case SimulationPhase.MatchesReady:
                //Debug.Log("Starting matchround " + Population.Generation + "." + (MatchesPlayed + 1));
                SimulationUI.TitleText.text = "Match Round " + Population.Generation + "." + (MatchesPlayed + 1);
                foreach (Match m in Matches)
                {
                    if(m.Visual)
                    {
                        SimulationUI.gameObject.SetActive(false);
                        VisualMatch = m;
                        m.StartMatch(VisualPlayer, VisualMinion, VisualBoardHeight, MatchUI);
                    }
                    else m.StartMatch();
                }
                SimulationPhase = SimulationPhase.MatchesRunning;
                break;

            case SimulationPhase.MatchesRunning:
                foreach (Match m in Matches) m.Update();
                if(Matches.TrueForAll(x => x.Phase == MatchPhase.GameEnded))
                {
                    MatchesPlayed++;
                    SimulationPhase = SimulationPhase.MatchesFinished;
                    if(VisualMatch != null)
                    {
                        VisualMatch = null;
                        SimulationUI.gameObject.SetActive(true);
                    }
                }
                break;

            case SimulationPhase.MatchesFinished:

                // Update Stats
                UpdateStatistics();

                if (MatchesPlayed >= MatchesPerGeneration)
                {
                    // Init Human vs AI game if gen is finished
                    if (SimulationUI.PlayGame.isOn)
                    {
                        SimulationUI.PlayGame.isOn = false;
                        Matches.Clear();

                        // Create match
                        Match match = new Match();
                        Player player1 = new HumanPlayer(match);
                        Subject bestSubject = Population.Subjects.OrderByDescending(x => x.Wins).First();
                        Player player2 = new AIPlayer(match, bestSubject);
                        match.InitGame(player1, player2, StartHealth, StartCardOptions, MinCardOptions, MaxCardOptions, MaxMinions, MaxMinionsPerType, FatigueDamageStartTurn, true, false);

                        // Start match
                        Matches.Add(match);
                        VisualMatch = match;
                        SimulationUI.gameObject.SetActive(false);
                        match.StartMatch(VisualPlayer, VisualMinion, VisualBoardHeight, MatchUI);
                        SimulationPhase = SimulationPhase.MatchesReady;
                    }

                    else
                    {
                        SimulationPhase = SimulationPhase.GenerationFinished;
                    }
                }
                else
                {
                    GenerateMatches();
                    SimulationPhase = SimulationPhase.MatchesReady;
                }
                break;

            case SimulationPhase.GenerationFinished:

                // Reset stats
                ResetStatistics();
                MatchesPlayed = 0;

                // Evolve and Update UI
                EvolutionInformation info = Population.EvolveGeneration();
                SimulationUI.EvoStats.UpdateStatistics(info);
                SimulationUI.SpeciesScoreboard.UpdateScoreboard(Population);

                // Generate first match round
                GenerateMatches();
                SimulationPhase = SimulationPhase.MatchesReady;
                break;
        }
    }

    private void DebugStandings()
    {

        Debug.Log("---------------- Standings after " + MatchesPlayed + " matches ----------------");
        foreach (Subject s in Population.Subjects.OrderByDescending(x => x.Wins))
        {
            Debug.Log(s.Name + ": " + s.Wins + "-" + s.Losses);
        }

    }

    #region Statistics

    private void UpdateStatistics()
    {
        UpdateMatchStatistics();
        UpdateCardStatistics();
    }

    /// <summary>
    /// Updates the match statistics according to the matches in the Matches list. Also updates the UI Element.
    /// </summary>
    private void UpdateMatchStatistics()
    {
        TotalMatches += Matches.Count;

        // Player 1 Winrate
        Player1WonMatches += Matches.Where(x => x.Winner == x.Player1).Count();
        float p1winrate = (float)Player1WonMatches / TotalMatches;

        // Game Length
        TotalTurns += Matches.Sum(x => x.Turn);
        float avgGameLength = (float)TotalTurns / TotalMatches;

        // Card Options
        WinnerNumCardOptions += Matches.Sum(x => x.Winner.NumCardOptions);
        LoserNumCardOptions += Matches.Sum(x => x.Loser.NumCardOptions);
        float avgWinnerCardOptions = (float)WinnerNumCardOptions / TotalMatches;
        float avgLoserCardOptions = (float)LoserNumCardOptions / TotalMatches;

        SimulationUI.MatchStatistics.UpdateStatistics(p1winrate, avgGameLength, avgWinnerCardOptions, avgLoserCardOptions);
    }

    /// <summary>
    /// Updates the card statistics according to the matches in the Matches list. Also updates the UI Element.
    /// </summary>
    private void UpdateCardStatistics()
    {
        foreach (Match m in Matches)
        {
            if (m.Phase != MatchPhase.GameEnded) throw new System.Exception("Match not finished");
            foreach (KeyValuePair<int, int> kvp in m.Player1.CardsPicked) CardsPicked[kvp.Key] += kvp.Value;
            foreach (KeyValuePair<int, int> kvp in m.Player2.CardsPicked) CardsPicked[kvp.Key] += kvp.Value;

            foreach (KeyValuePair<int, int> kvp in m.Player1.CardsNotPicked) CardsNotPicked[kvp.Key] += kvp.Value;
            foreach (KeyValuePair<int, int> kvp in m.Player2.CardsNotPicked) CardsNotPicked[kvp.Key] += kvp.Value;

            foreach (KeyValuePair<int, int> kvp in m.Winner.CardsPicked) CardsPickedByWinner[kvp.Key] += kvp.Value;
            foreach (KeyValuePair<int, int> kvp in m.Loser.CardsPicked) CardsPickedByLoser[kvp.Key] += kvp.Value;
        }

        CardPickedPerMatch.Clear();
        CardPickrate.Clear();
        CardWinrate.Clear();

        for (int i = 0; i < CardList.Cards.Count; i++)
        {
            CardPickedPerMatch.Add(i, (float)CardsPicked[i] / TotalMatches / 2); // The /2 is because it is per player
            CardPickrate.Add(i, (float)CardsPicked[i] / (CardsPicked[i] + CardsNotPicked[i]));
            CardWinrate.Add(i, (float)CardsPickedByWinner[i] / (CardsPickedByWinner[i] + CardsPickedByLoser[i]));
        }

        SimulationUI.UpdateCardStatistics(CardPickedPerMatch, CardPickrate, CardWinrate);
    }

    /// <summary>
    /// Resets all match and card statistics.
    /// </summary>
    private void ResetStatistics()
    {
        CardsPicked = new Dictionary<int, int>();
        CardsPickedByWinner = new Dictionary<int, int>();
        CardsPickedByLoser = new Dictionary<int, int>();
        CardsNotPicked = new Dictionary<int, int>();
        CardPickedPerMatch = new Dictionary<int, float>();
        CardPickrate = new Dictionary<int, float>();
        CardWinrate = new Dictionary<int, float>();
        for (int i = 0; i < CardList.Cards.Count; i++)
        {
            CardsPicked.Add(i, 0);
            CardsPickedByWinner.Add(i, 0);
            CardsPickedByLoser.Add(i, 0);
            CardsNotPicked.Add(i, 0);
        }
        Player1WonMatches = 0;
        WinnerNumCardOptions = 0;
        LoserNumCardOptions = 0;
        TotalMatches = 0;
        TotalTurns = 0;
    }

    #endregion
}
