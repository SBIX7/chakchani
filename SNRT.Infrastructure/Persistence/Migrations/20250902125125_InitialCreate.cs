using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SNRT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TitleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTitleOrders",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TitleItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTitleOrders", x => new { x.UserId, x.TitleItemId });
                    table.ForeignKey(
                        name: "FK_UserTitleOrders_TitleItems_TitleItemId",
                        column: x => x.TitleItemId,
                        principalTable: "TitleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTitleOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TitleItems",
                columns: new[] { "Id", "Description", "Title" },
                values: new object[,]
                {
                    { new Guid("0cdcb930-144e-459b-83c5-e35c2b5f1c62"), null, "Title 6" },
                    { new Guid("24a22e2b-6a09-42f2-a5d3-5e9ceb929de7"), null, "Title 9" },
                    { new Guid("3149a6a8-7996-4521-aef8-d245d96b5e69"), null, "Title 3" },
                    { new Guid("589bd436-7cf6-4207-90b2-090fba805646"), null, "Title 5" },
                    { new Guid("5c7bb5cd-1fe1-4758-b37d-8d17f81dca41"), null, "Title 1" },
                    { new Guid("60139737-d9db-4459-8282-59b2ab2b9b14"), null, "Title 4" },
                    { new Guid("62200d0b-c1cb-46af-a472-5d25ecc17faf"), null, "Title 11" },
                    { new Guid("7a3ab926-3976-4492-ad72-3b82e16c26a7"), null, "Title 7" },
                    { new Guid("871da67f-76a4-4ef6-b7db-3ae9fd384676"), null, "Title 10" },
                    { new Guid("a408e591-d18c-457a-aa69-77bc20e7ed58"), null, "Title 2" },
                    { new Guid("df1bb89d-7031-4ba4-9ccd-1368c4599f40"), null, "Title 8" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_UserId",
                table: "LoginLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTitleOrders_TitleItemId",
                table: "UserTitleOrders",
                column: "TitleItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginLogs");

            migrationBuilder.DropTable(
                name: "UserTitleOrders");

            migrationBuilder.DropTable(
                name: "TitleItems");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
