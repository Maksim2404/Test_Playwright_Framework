namespace test.playwright.framework.security.sql;

public static class SqlInjectionPayloads
{
    public static readonly string[] BasicPayloads =
    [
        "' OR '1'='1",
        "'; DROP TABLE users; --",
        "' UNION SELECT null, null, null --",
        "'; EXEC xp_cmdshell('dir') --",
        "admin' --",
        "' AND 'a'='a",
        "' OR 1=1 --"
    ];

    public static readonly string[] AdvancedPayloads =
    [
        "' OR EXISTS (SELECT * FROM users WHERE username='admin') --",
        "'; SELECT * FROM information_schema.tables; --",
        "' AND SLEEP(5) --",
        "'; UPDATE users SET role='admin' WHERE username='guest'; --"
    ];
}