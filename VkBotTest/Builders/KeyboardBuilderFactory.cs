using VkNet.Enums.StringEnums;
using VkNet.Model;

namespace VkBotTest.Builders;

public static class KeyboardBuilderFactory
{

    //Создаёт основную клавиатуру бота
    public static KeyboardBuilder CreateMainKeyboard()
    {
        var keyboard = new KeyboardBuilder();

        // Ряд 1
        keyboard.AddButton("Статистика", "/stats", KeyboardButtonColor.Primary);
        keyboard.AddButton("Помощь", "/help", KeyboardButtonColor.Secondary);
        keyboard.AddLine();
        // Ряд 2
        keyboard.AddButton("Проверка связи", "/ping", KeyboardButtonColor.Default);

        return keyboard;
    }

    // Создаёт клавиатуру для команды /start
    public static KeyboardBuilder CreateStartKeyboard()
    {
        var keyboard = new KeyboardBuilder();

        keyboard.AddButton("Статистика", "/stats", KeyboardButtonColor.Primary);
        keyboard.AddButton("Помощь", "/help", KeyboardButtonColor.Secondary);
        keyboard.AddLine();
        keyboard.AddButton("Очистить историю", "/clear", KeyboardButtonColor.Negative);

        return keyboard;
    }

    //Создаёт inline-клавиатуру (под сообщением)
    public static KeyboardBuilder CreateInlineKeyboard()
    {
        var keyboard = new KeyboardBuilder();
        keyboard.SetInline(true);

        keyboard.AddButton("Полезно", "callback_useful", KeyboardButtonColor.Positive);
        keyboard.AddButton("Не полезно", "callback_not_useful", KeyboardButtonColor.Negative);

        return keyboard;
    }
}