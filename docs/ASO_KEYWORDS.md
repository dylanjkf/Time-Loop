# TIME LOOP — ASO Keyword Strategy

Companion doc to `APP_STORE_LISTING.md`. Covers the iOS keyword field, the Android/Play indexing approach (a genuinely different mechanism, not the same list reused), comparable apps for discovery reasoning, and the "why" behind the top picks.

Assumed App Name / Subtitle (from `APP_STORE_LISTING.md`): **"Time Loop: Ghost Puzzle"** / **"Team Up With Your Past Self"**. Every list below is built to avoid wasting characters on words those two fields already cover.

---

## 1. iOS Keyword Field (App Store Connect — 100 characters, hidden from users)

**The rule, and why:** Apple has one dedicated 100-character "Keywords" field per app, entered comma-separated. It is never shown to users — it exists purely so Apple's search index can match it against what people type. Two conventions follow directly from how that field is parsed:

1. **No spaces after commas.** Apple's algorithm treats the field as a bag of individual words (and auto-generates combinations across App Name + Subtitle + Keywords to match multi-word queries) — it does not need a literal space to "read" two words as separate. A space after a comma is therefore a wasted character; `brain,teaser` and `brain, teaser` match identically, but the second one costs one extra character for nothing. Every list below is written with no space after the comma.
2. **Never repeat a word already in the App Name or Subtitle.** Those two fields are indexed for search automatically and get *more* ranking weight than the Keywords field, not less. Re-typing "puzzle," "ghost," "time," or "loop" into the 100-character field spends budget on words that are already fully indexed, at the cost of a word that isn't. So this list contains zero overlap with "Time Loop: Ghost Puzzle" / "Team Up With Your Past Self."

### Recommended field (97 / 100 characters)

```
brain,teaser,logic,riddle,mind,escape,room,clone,rewind,travel,clockwork,offline,coop,mystery,zen
```

Ordered roughly by priority (highest-value terms first is a minor hedge in case Apple ever truncates or in case of future re-editing, though position within the field does not itself affect ranking the way it does in the App Name).

---

## 2. Rationale: Why Each Keyword Was Chosen

| Keyword | Relevance | Volume intuition | Competition | Why it made the cut |
|---|---|---|---|---|
| `brain` | High | High | High | Anchors the enormous "brain teaser / brain games" search category. High competition, but the category volume is large enough that even a small capture rate matters, and it's a true description of the game. |
| `teaser` | High | High (paired w/ "brain") | Medium | Rarely searched completely alone, but Apple's combinatorial matching means `brain` + `teaser` in the same field covers "brain teaser," "teaser puzzle," and similar two-word queries without spending characters on the phrase itself. |
| `logic` | High | Medium-High | Medium | Precisely describes the genre ("logic puzzle") and skews toward the exact audience — deliberate, thoughtful puzzle players rather than casual match-3 traffic — so conversion-quality on this term should be strong even though raw volume is a step below "puzzle." |
| `riddle` | Medium-High | Medium | Medium | Captures "riddle"/"riddle game" searchers, a puzzle-adjacent audience that's under-served by most genre-generic ASO lists, so competition is lighter than the core `puzzle`/`brain` terms. |
| `mind` | Medium-High | Medium-High | Medium-High | Broad enough to combine with several other indexed words ("mind bending," "mind games," "mind puzzle") for very little character cost — a one-word multiplier. |
| `escape` | High | High | High | "Escape room" is one of the largest adjacent search categories in mobile puzzle discovery. Competition is stiff (a whole sub-genre of literal escape-room apps owns this term), but the category's sheer size still makes it worth the character spend — Time Loop's room-based structure is genuinely on-theme, not a stretch. |
| `room` | High | High (paired w/ "escape") | High | Pairs with `escape` for the "escape room" combination, and doubles as a literal, honest description of the game's structure (every level *is* a room). |
| `clone` | High (differentiator) | Low-Medium | Low | The single most mechanic-specific word on the list — describes the ghost/past-self mechanic in a word real searchers actually type ("clone puzzle game"). Lower volume than the generic terms, but also far less competitive, and anyone who does search it is a near-perfect-fit player. |
| `rewind` | Medium | Low-Medium | Low-Medium | Time-manipulation-genre term with a smaller, more targeted search base than "time travel" — catches Braid-adjacent searchers specifically. |
| `travel` | Medium-High | Medium | Medium | Doesn't stand alone, but combines with the already-indexed App Name word "Time" to cover "time travel game" — one of the more searched phrases in this genre — for the cost of a single word. |
| `clockwork` | Low-Medium (long-tail) | Low | Very Low | A deliberate long-tail pick: it's also the name of one of the paid cosmetic Timeline themes ("Ancient Clockwork"), so it does double duty — genre-adjacent search term *and* a hook for players specifically searching that aesthetic. Low volume, but essentially zero competition. |
| `offline` | Medium-High (intent-driven) | Medium | Low | A practical/functional search term ("offline puzzle games," "no wifi games") rather than a genre term — these searchers have high purchase intent (they want exactly this: a real single-player game they own, not a live-service app) and far less ASO competition fights over functional terms than over genre terms. |
| `coop` | High (core selling point) | Low-Medium | Low | Directly encodes the marketing hook ("cooperate with your own past self"). Standalone volume for "coop" skews toward multiplayer games, so some searches here won't convert, but the ones that do land are describing exactly what makes Time Loop unusual. |
| `mystery` | Low-Medium | Medium | Medium | Generic enough to pick up cross-shopping traffic from mystery/puzzle-adventure browsers; lowest-confidence pick on the list, included because it was the best use of the remaining ~9 characters over a second escape-room or brain-teaser synonym. |
| `zen` | Low-Medium | Low-Medium | Low | Targets the calm/minimalist side of the audience (the Monument-Valley-style "relaxing puzzle" searcher) rather than the challenge-seeking side — a cheap way to cover a second audience segment in 4 characters. |

**Deliberately excluded:** plurals of words already on the list (`puzzles`, `riddles`) — Apple's matching already handles simple stem variants reasonably well in English, so a duplicate plural is close to wasted budget; and any competitor app name (see §4) — using another app's trademark in the Keywords field risks an App Review rejection under Apple's trademark/metadata guidelines, so that kind of "ride the competitor's search traffic" tactic belongs in market research, not in the submitted field.

---

## 3. Android / Google Play Keyword Strategy (description-based indexing)

**The distinction that matters:** Google Play has **no dedicated keyword field at all.** There is nothing hidden to fill in. Instead, Play's search ranking is driven by ordinary on-page text — primarily the **Title**, **Short Description** (80 chars), and **Full Description** (4,000 chars) — weighted by keyword relevance, placement, and natural repetition ("density") within that visible copy. Practically, this means:

- Every keyword below needs to actually appear, worded naturally, somewhere a human will read it — not tucked into a hidden metadata field.
- Unlike iOS, there's no hard reason to avoid repeating a word from the Title in the description — repetition across Title + Short Description + Full Description is exactly what reinforces relevance in Play's model. (Play does penalize *unnatural* stuffing — e.g., a paragraph that's just a comma list of keywords — so density has to stay inside readable sentences.)
- Because the Full Description is fully user-visible (unlike iOS's hidden Keywords field), every phrase chosen here has to survive being read as marketing copy, not just as a search token.

### Priority phrase list for the Play description (work these into the Full Description naturally; the current draft in `APP_STORE_LISTING.md` already covers a subset)

**Core genre/category phrases**
- puzzle game
- brain teaser
- logic puzzle
- mind bending puzzle
- escape room game
- puzzle adventure

**Mechanic-specific phrases (Time Loop's actual differentiator — worth extra repetition since no competitor owns these)**
- time loop game
- time travel puzzle
- co-op with yourself
- past self
- ghost mechanic
- clone puzzle
- self cooperation
- record and replay

**Content/format phrases (match how players search once they've decided on the genre)**
- daily puzzle challenge
- offline puzzle game
- singleplayer puzzle game
- premium puzzle game
- no ads puzzle game
- one time purchase game
- infinite puzzle mode
- 150 levels

**Tone/aesthetic phrases (secondary audience segment)**
- relaxing puzzle game
- minimalist puzzle game
- indie puzzle game
- cyberpunk puzzle
- atmospheric puzzle game

**Practical guidance:** front-load the highest-value phrases (the "core genre" and "mechanic-specific" groups) into the first 1–2 sentences of the Full Description and into the Short Description, since Play's index — like most search engines — weights earlier placement more heavily than text buried at paragraph ten. The Short Description recommended in `APP_STORE_LISTING.md` ("Team up with your past self in this brain-teasing time-loop puzzle adventure.") was written specifically to front-load four of these phrases (`team up`/`past self`, `brain teaser`, `time loop`, `puzzle adventure`) into that 80-character field.

---

## 4. Comparable / Competitor Apps (for "more like this" discovery reasoning)

Useful for reasoning about audience overlap, cross-promotion targets, and which existing player bases to study — not for stuffing trademarked names into metadata fields (see the exclusion note in §2).

| App | Why it's a relevant comparable |
|---|---|
| **The Room** (series, Fireproof Games) | The clearest business-model and tone comparable: premium, tactile, atmospheric mobile puzzle-box game with a devoted paid-unlock audience — closest match for Time Loop's monetization pitch. |
| **Monument Valley** (1 & 2, ustwo Games) | Same premium one-time-purchase mobile model, same "short, gorgeous, calm" positioning; a large share of its audience is exactly Time Loop's target buyer. |
| **Baba Is You** | Shares the "one small rule set, escalating combinatorial depth, the player discovers the trick once" design philosophy explicitly cited in Time Loop's own design pillars. |
| **Braid** | The direct genre ancestor for "time manipulation as the core verb, not a gimmick" — worth citing even though its mobile presence is limited, since players who loved Braid are a strong cross-shopping signal on other storefronts (Steam wishlists, YouTube puzzle-game audiences) that also drive App Store search traffic. |
| **The Swapper** | The closest existing *mechanic* comparable: a puzzle game built entirely around creating clones of yourself and using them as physical tools — extremely useful for reasoning about who searches "clone puzzle game." |
| **Gorogoa** | Mobile-native, premium, minimalist, hand-crafted — a strong comparable for the "small, beautiful, one-purchase puzzle game" buyer persona on iOS/Android specifically. |
| **Framed** | Mobile-first premium indie puzzle game with a strong "aha" mechanic reveal (rearranging comic panels) — good comparable for players who discover puzzle games through the "clever twist" hook rather than through pure genre search. |
| **Rusty Lake** (series, incl. Cube Escape) | Represents the escape-room-adjacent mobile audience captured by the `escape`/`room` keywords — a mostly-premium/IAP puzzle-room series with a large, loyal mobile following. |

---

## 5. Summary Table (quick reference)

| Field | Platform | Limit | Status |
|---|---|---|---|
| App Name | iOS | 30 chars | "Time Loop: Ghost Puzzle" (23) |
| Subtitle | iOS | 30 chars | "Team Up With Your Past Self" (27) |
| Keywords | iOS | 100 chars | 15 words, 97 chars (§1) |
| Title | Android | 30 chars | Reuse App Name as-is |
| Short Description | Android | 80 chars | "Team up with your past self in this brain-teasing time-loop puzzle adventure." (77) |
| Full Description | Both | 4,000 chars | Shared copy in `APP_STORE_LISTING.md`, Play version enriched per §3 |
