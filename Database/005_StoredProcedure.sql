USE FinTrack;
GO

CREATE OR ALTER PROCEDURE sp_GetCategoryWiseReport
(
    @UserId INT,
    @Month INT,
    @Year INT
)
AS
BEGIN

    SET NOCOUNT ON;

    SELECT

        c.CategoryName,

        SUM(t.Amount) AS TotalAmount

    FROM Transactions t

    INNER JOIN Categories c
        ON t.CategoryId = c.CategoryId

    INNER JOIN CategoryTypes ct
        ON c.CategoryTypeId = ct.CategoryTypeId

    WHERE

        t.UserId = @UserId

        AND ct.TypeName='Expense'

        AND MONTH(t.TransactionDate)=@Month

        AND YEAR(t.TransactionDate)=@Year

    GROUP BY

        c.CategoryName

    ORDER BY

        TotalAmount DESC;

END
GO