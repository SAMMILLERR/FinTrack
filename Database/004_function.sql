ALTER FUNCTION fn_GetMonthlyTotal
(
    @UserId INT,
    @Month INT,
    @Year INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        ISNULL(
            SUM(
                CASE
                    WHEN ct.TypeName = 'Income'
                    THEN t.Amount
                    ELSE 0
                END
            ), 0
        ) AS TotalIncome,

        ISNULL(
            SUM(
                CASE
                    WHEN ct.TypeName = 'Expense'
                    THEN t.Amount
                    ELSE 0
                END
            ), 0
        ) AS TotalExpense

    FROM Transactions t

    INNER JOIN Categories c
        ON t.CategoryId = c.CategoryId

    INNER JOIN CategoryTypes ct
        ON c.CategoryTypeId = ct.CategoryTypeId

    WHERE
        t.UserId = @UserId
        AND MONTH(t.TransactionDate) = @Month
        AND YEAR(t.TransactionDate) = @Year
);