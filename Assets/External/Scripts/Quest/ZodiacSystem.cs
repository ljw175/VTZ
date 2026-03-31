using UnityEngine;

public enum ZodiacSign 
{ 
    Capricorn, Aquarius, Pisces, Aries, Taurus, Gemini, 
    Cancer, Leo, Virgo, Libra, Scorpio, Sagittarius 
}

public static class ZodiacSystem
{
    // 월/일에 따른 별자리 계산
    public static ZodiacSign GetZodiacSign(int month, int day)
    {
        switch (month)
        {
            case 1: return day <= 19 ? ZodiacSign.Capricorn : ZodiacSign.Aquarius;
            case 2: return day <= 18 ? ZodiacSign.Aquarius : ZodiacSign.Pisces;
            case 3: return day <= 20 ? ZodiacSign.Pisces : ZodiacSign.Aries;
            case 4: return day <= 19 ? ZodiacSign.Aries : ZodiacSign.Taurus;
            case 5: return day <= 20 ? ZodiacSign.Taurus : ZodiacSign.Gemini;
            case 6: return day <= 21 ? ZodiacSign.Gemini : ZodiacSign.Cancer;
            case 7: return day <= 22 ? ZodiacSign.Cancer : ZodiacSign.Leo;
            case 8: return day <= 22 ? ZodiacSign.Leo : ZodiacSign.Virgo;
            case 9: return day <= 22 ? ZodiacSign.Virgo : ZodiacSign.Libra;
            case 10: return day <= 23 ? ZodiacSign.Libra : ZodiacSign.Scorpio;
            case 11: return day <= 22 ? ZodiacSign.Scorpio : ZodiacSign.Sagittarius;
            case 12: return day <= 21 ? ZodiacSign.Sagittarius : ZodiacSign.Capricorn;
            default: return ZodiacSign.Aries;
        }
    }

    // 별자리 이름 텍스트 반환 (UI 출력용)
    public static string GetZodiacName(ZodiacSign sign)
    {
        switch (sign)
        {
            case ZodiacSign.Capricorn: return "염소의 달";
            case ZodiacSign.Aquarius: return "물병의 달";
            case ZodiacSign.Pisces: return "물고기의 달";
            case ZodiacSign.Aries: return "양의 달";
            case ZodiacSign.Taurus: return "황소의 달";
            case ZodiacSign.Gemini: return "쌍둥이의 달";
            case ZodiacSign.Cancer: return "게의 달";
            case ZodiacSign.Leo: return "사자의 달";
            case ZodiacSign.Virgo: return "처녀의 달";
            case ZodiacSign.Libra: return "천칭의 달";
            case ZodiacSign.Scorpio: return "전갈의 달";
            case ZodiacSign.Sagittarius: return "궁수의 달";
            default: return sign.ToString();
        }
    }
}