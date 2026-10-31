using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bookism.Migrations
{
    /// <inheritdoc />
    public partial class SyncBookshelfAndRealWorldUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop existing constraints between Todos and Users
            migrationBuilder.DropForeignKey(
                name: "FK_Todos_Users_UserId",
                table: "Todos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Todos",
                table: "Todos");

            // 2. Rename tables to snake_case
            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Todos",
                newName: "todos");

            // 3. Rename columns of users to snake_case
            migrationBuilder.RenameColumn(
                name: "Username",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "users",
                newName: "ix_users_username");

            // 4. Rename columns of todos to snake_case
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "todos",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "todos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "todos",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "IsCompleted",
                table: "todos",
                newName: "is_completed");

            migrationBuilder.RenameIndex(
                name: "IX_Todos_UserId",
                table: "todos",
                newName: "ix_todos_user_id");

            // 5. Add new columns to users
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "user");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            // 6. Re-create primary keys
            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_todos",
                table: "todos",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            // 7. Re-create foreign key for todos -> users
            migrationBuilder.AddForeignKey(
                name: "fk_todos_users_user_id",
                table: "todos",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // 8. Seed/Update existing user data:
            // Assign unique placeholder emails and set 'brandnewshvt' as admin
            migrationBuilder.Sql(@"
                UPDATE users 
                SET email = username || '@example.com',
                    role = CASE WHEN username = 'brandnewshvt' THEN 'admin' ELSE 'user' END
                WHERE email IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_todos_users_user_id",
                table: "todos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_todos",
                table: "todos");

            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "is_completed",
                table: "todos",
                newName: "IsCompleted");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "todos",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "todos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "todos",
                newName: "Title");

            migrationBuilder.RenameIndex(
                name: "ix_todos_user_id",
                table: "todos",
                newName: "IX_Todos_UserId");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "users",
                newName: "Username");

            migrationBuilder.RenameIndex(
                name: "ix_users_username",
                table: "users",
                newName: "IX_Users_Username");

            migrationBuilder.RenameTable(
                name: "todos",
                newName: "Todos");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Todos",
                table: "Todos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Todos_Users_UserId",
                table: "Todos",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
