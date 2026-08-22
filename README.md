# Constellation Sketch

Creates sketch worksheets for the constellations in the northern hemisphere.

Designed for the [Astronomical League's Constellation Hunter Observing Program](https://www.astroleague.org/constellation-hunter-observing-program/)

## Data Sources

### Constellation Boundaries

Constellation boundary polygons are derived from the official [IAU constellation boundary data](https://www.iau.org/IAU/IAU/Astronomy-FAQs/Constellations.aspx), originally defined by Eugène Delporte in *Délimitation scientifique des constellations* (1930) and later digitized by A.C. Davenhall and S.K. Leggett. The boundary vertex coordinates use the J2000.0 epoch and are stored as pipe-delimited text files in `constellation/wwwroot/boundaries/`.

### Star Catalog

Star positions and visual magnitudes are sourced from the [HYG Stellar Database](https://github.com/astronexus/HYG-Database) (v4.1) by David Nash, which combines data from the Hipparcos catalog, the Yale Bright Star Catalogue, and the Gliese Catalogue of Nearby Stars. The catalog is filtered to stars of visual magnitude 6.0 or brighter (approximately 5,000 stars visible to the naked eye). Coordinates use the J2000.0 epoch. The HYG Database is licensed under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/).