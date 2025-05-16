public interface IUserRepository
{
    User? GetByLogin(string login);
    User? GetByLoginAndPassword(string login, string password);
    IEnumerable<User> GetAllActive();
    void Update(User user);
    void Delete(User user);
    IEnumerable<User> GetUsersOlderThan(int age);
    User Add(User user);
}
