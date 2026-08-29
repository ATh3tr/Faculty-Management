export const arabicTextPattern = "[^A-Za-z]*[\\u0600-\\u06FF\\u0750-\\u077F\\u08A0-\\u08FF][^A-Za-z]*";
export const englishTextPattern = "[^\\u0600-\\u06FF\\u0750-\\u077F\\u08A0-\\u08FF]*[A-Za-z][^\\u0600-\\u06FF\\u0750-\\u077F\\u08A0-\\u08FF]*";
export const strictEmailPattern = "[^\\s@]+@[^\\s@]+\\.[^\\s@.]{2,}";
export const asciiDigitsPattern = "[0-9]+";

const arabicCharacters = /[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF]/;
const latinCharacters = /[A-Za-z]/;

export function validateBilingual(arabic: string, english: string, fieldName: string) {
  if (!arabicCharacters.test(arabic) || latinCharacters.test(arabic))
    throw new Error(`Arabic ${fieldName} must contain Arabic text and no English letters.`);
  if (!latinCharacters.test(english) || arabicCharacters.test(english))
    throw new Error(`English ${fieldName} must contain English text and no Arabic characters.`);
}
