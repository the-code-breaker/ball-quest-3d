"""Verify the responsive-controls APK against this revision's actual Unity results."""
from VerifyManualRelease import ROOT, main

if __name__ == "__main__":
    reports=["TestResults/EditMode-v6.xml", "TestResults/PlayMode-v6-full.xml"]
    final="TestResults/FinalChecks-v6.xml"
    if (ROOT/final).exists():
        reports.append(final)
    main(version="0.6.0", version_code=6,
         artifact="BallQuest3D-Responsive-v0.6.0.apk",
         reports=reports, result_filename="responsive-release.json", expected_tests=87)
