using AutoEvent;

for (int roundsStarted = 0; roundsStarted < 12; roundsStarted++)
    Assert(AutoVotingRules.ShouldVote(roundsStarted, 4) == (roundsStarted is 3 or 7 or 11), $"Неверный раунд голосования: {roundsStarted + 1}");

Assert(!AutoVotingRules.ShouldVote(3, 0), "Нулевой период не должен запускать голосование");

var votes = new Dictionary<string, bool>();
Assert(!AutoVotingRules.Passed(votes), "Голосование без голосов не должно пройти");
Assert(AutoVotingRules.RecordVote(votes, "player-a", true), "Первый голос принят");
Assert(!AutoVotingRules.RecordVote(votes, "player-a", true), "Повторный голос игнорируется");
Assert(votes.Count == 1 && AutoVotingRules.Passed(votes), "Один игрок имеет один голос");
Assert(AutoVotingRules.RecordVote(votes, "player-b", false), "Отрицательный голос принят");
Assert(!AutoVotingRules.Passed(votes), "Ничья не должна пройти");
Assert(AutoVotingRules.RecordVote(votes, "player-a", false), "Игрок может изменить голос");
Assert(!AutoVotingRules.Passed(votes), "После смены выбора голосов «за» нет");
votes.Remove("player-b");
Assert(votes.Count == 1, "Голос вышедшего игрока удалён");
votes["player-a"] = true;
Assert(AutoVotingRules.Passed(votes), "Большинство голосов «да» проходит");

Console.WriteLine("Проверки подсчёта раундов и голосов пройдены.");

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
