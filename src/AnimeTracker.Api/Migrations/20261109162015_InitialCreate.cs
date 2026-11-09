using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AnimeTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    GenreId = table.Column<int>(type: "INTEGER", nullable: true),
                    CoverFile = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Series_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MediaType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitLabel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TotalUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    WatchedUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ExternalUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ImageFile = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                    table.CheckConstraint("CK_Entry_Completed_Full", "\"Status\" <> 'Completed' OR \"WatchedUnits\" = \"TotalUnits\"");
                    table.CheckConstraint("CK_Entry_TotalUnits_Positive", "\"TotalUnits\" > 0");
                    table.CheckConstraint("CK_Entry_WatchedUnits_Range", "\"WatchedUnits\" >= 0 AND \"WatchedUnits\" <= \"TotalUnits\"");
                    table.ForeignKey(
                        name: "FK_Entries_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntryTag",
                columns: table => new
                {
                    EntriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryTag", x => new { x.EntriesId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_EntryTag_Entries_EntriesId",
                        column: x => x.EntriesId,
                        principalTable: "Entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntryTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Genres",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Isekai" },
                    { 2, "Hentai" },
                    { 3, "Romance" },
                    { 4, "Fantasy" },
                    { 5, "Shounen" },
                    { 6, "Action" },
                    { 7, "Comedy" },
                    { 8, "Slice of life" },
                    { 9, "Sci-Fi" },
                    { 10, "Horror" },
                    { 11, "Supernatural" },
                    { 12, "Sports" },
                    { 13, "Mystery" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_SeriesId_SortOrder",
                table: "Entries",
                columns: new[] { "SeriesId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EntryTag_TagsId",
                table: "EntryTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_GenreId",
                table: "Series",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntryTag");

            migrationBuilder.DropTable(
                name: "Entries");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Series");

            migrationBuilder.DropTable(
                name: "Genres");
        }
    }
}
