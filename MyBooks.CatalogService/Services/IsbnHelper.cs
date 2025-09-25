namespace MyBooks.CatalogService.Services;

public static class IsbnHelper
{
    // helper: check if isbn contains only digits, dash, space, or X at the end
    public static bool IsPlausibleIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        isbn = isbn.Trim();
        for (int i = 0; i < isbn.Length; i++)
        {
            char c = isbn[i];
            if (char.IsDigit(c) || c == '-' || c == ' ')
                continue;

            if (c == 'X' && i == isbn.Length - 1)
                continue;

            return false;
        }

        return true;
    }
}