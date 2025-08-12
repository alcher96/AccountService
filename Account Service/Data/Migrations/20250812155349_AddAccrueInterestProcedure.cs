using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccrueInterestProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE OR REPLACE PROCEDURE accrue_interest(account_id UUID)
            LANGUAGE plpgsql
            AS $$
            DECLARE
                account RECORD;
                daily_rate DECIMAL;
                interest DECIMAL;
            BEGIN
                SELECT * INTO account
                FROM ""Accounts""
                WHERE ""AccountId"" = account_id
                FOR UPDATE;

                IF account IS NULL THEN
                    RAISE EXCEPTION 'Account % not found', account_id;
                END IF;

                IF account.""AccountType"" != 1 THEN
                    RETURN;
                END IF;

                daily_rate := account.""InterestRate"" / 365;
                interest := account.""Balance"" * daily_rate;

                UPDATE ""Accounts""
                SET ""Balance"" = ""Balance"" + interest
                WHERE ""AccountId"" = account_id;

                INSERT INTO ""Transactions"" (
                    ""TransactionId"",
                    ""AccountId"",
                    ""Amount"",
                    ""Type"",
                    ""DateTime"",
                    ""Description""
                )
                VALUES (
                    gen_random_uuid(),
                    account_id,
                    interest,
                    1,
                    CURRENT_TIMESTAMP,
                    'Daily interest accrual'
                );
            END;
            $$;
        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS accrue_interest(UUID);");
        }
    }
}
