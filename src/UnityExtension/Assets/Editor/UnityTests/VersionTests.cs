using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using GitHub.Unity;

public class VersionTests
{
    [Test]
    public void OnePart_IsValid()
    {
        var version = TheVersion.Parse("2");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual(0, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(0, version.Build);
        Assert.AreEqual(null, version.Special);
    }

    [Test]
    public void TwoParts_IsValid()
    {
        var version = TheVersion.Parse("2.1");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(0, version.Build);
        Assert.AreEqual(null, version.Special);
    }

    [Test]
    public void ThreeParts_IsValid()
    {
        var version = TheVersion.Parse("2.1.32");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(32, version.Patch);
        Assert.AreEqual(0, version.Build);
        Assert.AreEqual(null, version.Special);
    }

    [Test]
    public void FourParts_IsValid()
    {
        var version = TheVersion.Parse("2.1.32.5");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(32, version.Patch);
        Assert.AreEqual(5, version.Build);
        Assert.AreEqual(null, version.Special);
    }

    [Test]
    public void TwoPartsWithAlpha_IsValid()
    {
        var version = TheVersion.Parse("2.1alpha1");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual("1alpha1", version.Special);
        Assert.AreEqual(0, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(0, version.Build);
    }

    [Test]
    public void ThreePartsWithAlpha_IsValid()
    {
        var version = TheVersion.Parse("2.1.3beta");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual("3beta", version.Special);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(0, version.Build);
    }

    [Test]
    public void FourPartsWithAlpha_IsValid()
    {
        var version = TheVersion.Parse("2.1.32.delta");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(32, version.Patch);
        Assert.AreEqual(0, version.Build);
        Assert.AreEqual("delta", version.Special);
    }

    [Test]
    public void ParsingStopsAtAlpha()
    {
        var version = TheVersion.Parse("2.1.1beta2.3");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual(1, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(0, version.Build);
        Assert.AreEqual("1beta2", version.Special);

        version = TheVersion.Parse("2.3beta2.3alpha.4");
        Assert.AreEqual(2, version.Major);
        Assert.AreEqual("3beta2", version.Special);
        Assert.AreEqual(0, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(0, version.Build);
    }

    [Test]
    public void EqualsWorks()
    {
        var version1 = TheVersion.Parse("2.1");
        var version2 = TheVersion.Parse("2.1.0.0");
        Assert.AreEqual(version1, version2);

        version1 = TheVersion.Parse("2");
        version2 = TheVersion.Parse("2.0.0.0");
        Assert.AreEqual(version1, version2);

        version1 = TheVersion.Parse("2.1.3");
        version2 = TheVersion.Parse("2.1.3.0");
        Assert.AreEqual(version1, version2);

        version1 = TheVersion.Parse("2.alpha1.1.2");
        version2 = TheVersion.Parse("2.alpha1");
        Assert.AreEqual(version1, version2);

        version1 = TheVersion.Parse("2.1.alpha1.1");
        version2 = TheVersion.Parse("2.1.alpha1");
        Assert.AreEqual(version1, version2);

        version1 = TheVersion.Parse("2.1.3.alpha1");
        version2 = TheVersion.Parse("2.1.3.alpha1");
        Assert.AreEqual(version1, version2);
    }

    [Test]
    public void ComparisonWorks()
    {
        var version1 = TheVersion.Parse("2");
        var version2 = TheVersion.Parse("1");
        Assert.IsTrue(version1 > version2);

        version1 = TheVersion.Parse("1");
        version2 = TheVersion.Parse("1.1alpha1");
        Assert.IsTrue(version2 > version1);

        version1 = TheVersion.Parse("1.0");
        version2 = TheVersion.Parse("1.1alpha1");
        Assert.IsTrue(version1 < version2);

        version1 = TheVersion.Parse("1.1");
        version2 = TheVersion.Parse("1.1alpha1");
        Assert.IsTrue(version1 > version2);

        version1 = TheVersion.Parse("1.1");
        version2 = TheVersion.Parse("1.2.3alpha1");
        Assert.IsTrue(version1 <= version2);

        version1 = TheVersion.Parse("1.2.3");
        version2 = TheVersion.Parse("1.2.3alpha1");
        Assert.IsTrue(version1 >= version2);

        version1 = TheVersion.Parse("0.32.0");
        version2 = TheVersion.Parse("0.33.0-beta");
        Assert.IsTrue(version1 < version2);
    }

    [Test]
    public void DetectingUnstableVersionsWorks()
    {
        var version = TheVersion.Parse("2");
        Assert.IsFalse(version.IsUnstable);

        version = TheVersion.Parse("1.2");
        Assert.IsFalse(version.IsUnstable);

        version = TheVersion.Parse("1.2.3");
        Assert.IsFalse(version.IsUnstable);

        version = TheVersion.Parse("1.2.3.4");
        Assert.IsFalse(version.IsUnstable);

        version = TheVersion.Parse("1.2alpha1");
        Assert.IsTrue(version.IsUnstable);

        version = TheVersion.Parse("1.2.3stuff");
        Assert.IsTrue(version.IsUnstable);

        version = TheVersion.Parse("1.2.3.4whatever");
        Assert.IsTrue(version.IsUnstable);
    }
}
