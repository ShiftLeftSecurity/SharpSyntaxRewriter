public record Person {
    public Person(string FirstName) { FirstName = FirstName; }
    public string FirstName { get; init; }
}