public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public User Add(User user)
    {
        _users.Add(user);
        return user;
    }

    // public User? GetByLogin(string login)
    //     => _users.FirstOrDefault(u => u.Login == login);
    
    public User? GetByLogin(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
            return null;

        return _users.FirstOrDefault(u => 
            u.Login.Equals(login.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public User? GetByLoginAndPassword(string login, string password)
    => _users.FirstOrDefault(u =>
        u.Login == login &&
        u.Password == password &&
        u.RevokedOn == null);

    public IEnumerable<User> GetAllActive()
        => _users.Where(u => u.RevokedOn == null).OrderBy(u => u.CreatedOn);

    public void Update(User user)
    {
        var index = _users.FindIndex(u => u.Id == user.Id);
        if (index != -1)
            _users[index] = user;
    }

    public void Delete(User user)
        => _users.Remove(user);

    public IEnumerable<User> GetUsersOlderThan(int age)
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-age);
        return _users.Where(u => u.Birthday <= cutoffDate);
    }
}

