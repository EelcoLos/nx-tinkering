namespace ApiDemo;

internal record Over18;
internal record NotOver18;

internal union AgeEligibility(Over18, NotOver18);
