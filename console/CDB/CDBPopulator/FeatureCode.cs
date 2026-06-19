using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace CDBPopulator
{
    /// <summary>
    /// A feature code (FC) used to classify features in the CDB standard.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The feature code is a five-character code where the first character
    /// represents a category of features, the second represents a subcategory
    /// of the current category, and the last three characters represent a
    /// specific type in the subcategory.
    /// </para>
    /// <para>
    /// The full list of feature codes is available in the <c>Feature_Data_Dictionary.xml</c>
    /// file in a CDB Metadata directory.
    /// </para>
    /// <para>
    /// This code implements Section 3.3.8.1 Feature Classification from the CDB standard,
    /// volume 1.
    /// </para>
    /// </remarks>
    /// <param name="Category">The feature category.</param>
    /// <param name="Subcategory">The feature subcategory of the category.</param>
    /// <param name="Type">The feature type in the subcategory.</param>
    /// <seealso href="https://github.com/opengeospatial/cdb-volume-1"/>
    public record FeatureCode([MaxLength(1)] string Category, [MaxLength(1)] string Subcategory, [Range(0, 999)] int Type)
    {
        /// <summary>
        /// Matches directory names of the form <c>A_Category</c>,
        /// where the <c>A</c> is the first character of a feature code,
        /// and <c>Category</c> is the name of the category identified
        /// by the code.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The capture groups of this pattern are <c>category</c> and <c>name</c>.
        /// </para>
        /// </remarks>
        public static Regex CategoryDirectoryPattern
        {
            get;
        } = new("^(?<category>[A-Z])_(?<name>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

        /// <summary>
        /// Matches directory names of the form <c>B_Subcategory</c>,
        /// where the <c>B</c> is the second character of the feature code,
        /// and <c>Subcategory</c> is the name of the subcategory identified
        /// by the code.  The subcategory is relative to the category.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The capture groups of this pattern are <c>subcategory</c> and <c>name</c>.
        /// </para>
        /// </remarks>
        public static Regex SubcategoryDirectoryPattern
        {
            get;
        } = new("^(?<subcategory>[A-Z])_(?<name>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

        /// <summary>
        /// Matches directory names of the form <c>999_Type</c>,
        /// where the <c>999</c> is the last three characters of the feature code,
        /// and <c>Type</c> is the name of the feature type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The capture groups of this pattern are <c>type</c> and <c>name</c>.
        /// Capture group <c>type</c> can be parsed as a non-negative integer.
        /// </para>
        /// </remarks>
        public static Regex TypeDirectoryPattern
        {
            get;
        } = new("^(?<type>\\d{3})_(?<name>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

        /// <summary>
        /// The code to call for every leaf directory found in the directory hierarchy.
        /// </summary>
        /// <param name="featureCode">The feature code represented by the directory hierarchy.</param>
        /// <param name="directory">The leaf directory of the hierarchy corresponding to the feature code.</param>
        public delegate void Act(FeatureCode featureCode, DirectoryInfo directory);

        /// <summary>
        /// Walks a directory tree matching that described in the CDB specification
        /// volume 1, Section 3.3.8.1. Feature Classification.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This walks a directory hierarchy conforming to the pattern <c>/A_Category/B_Subcategory/999_Type/</c>.
        /// </para>
        /// </remarks>
        /// <param name="dir">The root directory containing child directories of the form <c>/A_Category/B_Subcategory/999_Type/</c>.</param>
        /// <param name="action">The action to take for every leaf directory in the directory hierarchy.</param>
        public static void WalkDirectories(DirectoryInfo dir, Act action)
        {
            foreach (DirectoryInfo categoryDirectory in dir.EnumerateDirectories("*", EnumerationOptions))
            {
                Match categoryDirectoryMatch = CategoryDirectoryPattern.Match(categoryDirectory.Name);
                if (categoryDirectoryMatch.Success)
                {
                    string categoryCode = categoryDirectoryMatch.Groups["category"].Value;
                    string categoryName = categoryDirectoryMatch.Groups["name"].Value;

                    foreach (DirectoryInfo subcategoryDirectory in categoryDirectory.EnumerateDirectories("*", EnumerationOptions))
                    {
                        Match subcategoryDirectoryMatch = SubcategoryDirectoryPattern.Match(subcategoryDirectory.Name);
                        if (subcategoryDirectoryMatch.Success)
                        {
                            string subcategoryCode = subcategoryDirectoryMatch.Groups["subcategory"].Value;
                            string subcategoryName = subcategoryDirectoryMatch.Groups["name"].Value;

                            foreach (DirectoryInfo typeDirectory in subcategoryDirectory.EnumerateDirectories("*", EnumerationOptions))
                            {
                                Match typeDirectoryMatch = TypeDirectoryPattern.Match(typeDirectory.Name);
                                if (typeDirectoryMatch.Success)
                                {
                                    int typeCode = int.Parse(typeDirectoryMatch.Groups["type"].Value, CultureInfo.InvariantCulture);
                                    string typeName = typeDirectoryMatch.Groups["name"].Value;

                                    FeatureCode featureCode = new(categoryCode, subcategoryCode, typeCode);
                                    action(featureCode, typeDirectory);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static EnumerationOptions EnumerationOptions
        {
            get;
        } = new()
        {
            MatchCasing = MatchCasing.CaseInsensitive,
            MatchType = MatchType.Simple,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
        };

        /// <summary>
        /// The five-character code.
        /// </summary>
        public string Code => $"{Category}{Subcategory}{Type:D3}";

        public string GetDirectoryHierarchy()
        {
            // TODO: Need the metadata dir to construct these names.
            return Path.Combine($"{Category}_Name", $"{Subcategory}_Name", $"{Type:D3}_Name");
        }
    }
}
