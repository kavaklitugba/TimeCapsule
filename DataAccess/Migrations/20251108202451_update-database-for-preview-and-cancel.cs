using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class updatedatabaseforpreviewandcancel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimeCapsuleMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LookupId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderEmailEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SenderEmailIv = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    RecipientEmailEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    RecipientEmailIv = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SubjectEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SubjectIv = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    EncryptedBody = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    BodyIv = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SenderEmailHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    RecipientEmailHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SubjectHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SendAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeCapsuleMessages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeCapsuleMessages");
        }
    }
}
