using System.Collections.Generic;

namespace LuxuzDev.PersonalLogger;

public interface ITelegramBotSettings
{
    string BotToken { get; set; }
    List<long> BotAdmins { get; set; }
    string BotNameProject { get; set; }
    string BotPassword {get; set;}
}