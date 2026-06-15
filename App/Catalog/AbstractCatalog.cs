namespace DigitalBoardGameList.App.Catalog;

public abstract class AbstractCatalog<T>
{
    public IReadOnlyList<T> Games { get; }

    protected AbstractCatalog(IReadOnlyList<T> games)
    {
        Games = games;
    }
}