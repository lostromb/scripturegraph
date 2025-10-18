using ScriptureGraph.Core.Schemas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptureGraph.Core.Training
{
    internal static class ConferenceMetadata
    {
        internal static bool DoesTalkExist(Conference conf, string talkId)
        {
            IReadOnlyList<string>? talksInThisConf;
            if (!ALL_KNOWN_TALKS.TryGetValue(conf, out talksInThisConf))
            {
                // Unknown conference (in the future?)
                // Assume true
                return true;
            }

            return talksInThisConf.Contains(talkId, StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyDictionary<Conference, IReadOnlyList<string>> ALL_KNOWN_TALKS = new Dictionary<Conference, IReadOnlyList<string>>()
        {
            {
                new Conference(ConferencePhase.April, 1971),
                new List<string>()
                {
                    "a-witness-and-a-blessing","all-may-share-in-adams-blessing","be-slow-to-anger","choose-you-this-day","drink-of-the-pure-water","eternal-joy-is-eternal-growth","except-the-lord-build-the-house","great-experiences","help-needed-in-the-shaded-areas","honesty-and-integrity","ignorance-is-expensive","in-the-mountain-of-the-lords-house","kingdom-of-god","life-is-eternal","lost-battalions","love-of-god","love-of-the-right","man-cannot-endure-on-borrowed-light","marriage-is-intended-to-be-forever","my-brothers-keeper","our-responsibilities-as-priesthood-holders","out-of-the-darkness","practicing-what-we-preach","prepare-every-needful-thing","satan-the-great-deceiver","search-for-the-wanderers","teach-one-another","the-iron-rod","the-law-of-abundance","the-lords-people-receive-revelation","the-meaning-of-morality","the-message-of-the-restoration","the-spirit-beareth-record","thou-shalt-not-commit-adultery","todays-young-people","unchanging-principles-of-leadership","voices-of-the-past-of-the-present-of-the-future","warnings-from-the-past","when-thou-art-converted","where-are-you-really-going","ye-shall-not-fear","young-people-learn-wisdom-in-thy-youth"
                }
            },
            {
                new Conference(ConferencePhase.October, 1971),
                new List<string>()
                {
                    "a-new-health-missionary-program","a-time-of-testing","blessings-of-the-priesthood","by-love-serve-one-another","confession-and-forsaking-elements-of-genuine-repentance","continuity-of-service","decisions","elijah-the-prophet","glimpses-of-heaven","honesty-a-principle-of-salvation","how-to-worship","i-know-that-my-redeemer-liveth","i-was-in-prison-and-ye-came-unto-me","if-ye-be-willing-and-obedient","laying-a-foundation-for-the-millennium","let-the-spirit-of-oneness-prevail","love-unconditional","our-responsibility-to-save-the-world","responsibilities-of-the-priesthood","sacrifice-still-brings-forth-blessings","satans-thrust-youth","should-the-commandments-be-rewritten","strengthen-thy-brethren","strive-for-excellence","the-light-shineth","the-living-christ","the-nobility-of-man-in-choosing-good-over-evil","the-only-true-and-living-church","the-purpose-of-life-to-be-proved","the-sifting","the-ten-commandments","the-things-that-matter-most","the-vitality-of-love","this-is-my-beloved-son","this-same-jesus","thou-shalt-not","thus-saith-the-lord","turn-heavenward-our-eyes","watch-that-ye-may-be-ready","what-is-a-teacher","where-art-thou","with-hand-and-heart","you-can-get-there-from-here"
                }
            },
            {
                new Conference(ConferencePhase.April, 1972),
                new List<string>()
                {
                    "a-challenge-to-the-priesthood","a-foundation-whereon-men-cannot-fall","a-missionary-and-his-message","a-people-of-sound-judgment","a-prophets-blessing","a-teacher","a-time-of-decision","am-i-my-brothers-keeper","be-a-missionary-always-everywhere-you-go","civic-standards-for-the-faithful-saints","counsel-to-the-saints-and-to-the-world","courts-of-love","eternal-keys-and-the-right-to-preside","finishers-wanted","joy-through-christ","judge-not-that-ye-be-not-judged","keep-the-lines-of-communication-strong","know-thyself-control-thyself-give-thyself","knowing-god","mans-eternal-horizon","medicine-for-the-soul","missionary-training-begins-early","our-witness-to-the-world","peace","priesthood-its-power-and-vitality","salvation-and-exaltation","setting-the-example-in-the-home","successful-parenthood-a-noteworthy-accomplishment","the-aaronic-priesthood-a-sure-foundation","the-covenant-of-the-priesthood","the-fullness-of-the-gospel-in-each-mans-language","the-importance-of-a-personal-testimony","the-importance-of-prayer","the-last-dispensation","the-miracle-of-missionary-work","the-priesthood-a-royal-army","the-strength-of-the-priesthood","the-testimony-of-jesus","the-true-church","we-are-called-of-god","what-is-your-destination","what-will-the-church-do-for-you-a-man","whence-cometh-our-peace","why-stay-morally-clean"
                }
            },
            {
                new Conference(ConferencePhase.October, 1972),
                new List<string>()
                {
                    "a-blessing-for-the-saints","admonitions-for-the-priesthood-of-god","altar-tent-well","an-instrument-in-the-hands-of-the-lord","another-prophet-now-has-come","becoming-a-somebody","by-love-serve-one-another","caring-for-the-poor-and-needy","entrance-into-the-kingdom-of-god","every-man-in-his-own-place","genealogy-a-personal-responsibility","hands","harmony-in-the-home","having-been-born-of-goodly-parents","home-teachers-watchmen-over-the-church","how-to-gain-a-testimony","i-know-that-my-redeemer-lives","keep-the-commandments","light-and-knowledge-to-the-world","listen-to-a-prophets-voice","live-above-the-law-to-be-free","may-the-kingdom-of-god-go-forth","planting-gospel-seeds-of-spirituality","pollution-of-the-mind","spiritual-famine","strange-creeds-of-christendom","strengthen-thy-brethren","teach-the-gospel-of-salvation","the-priesthood-and-its-presidency","the-saints-securely-dwell","the-sure-word-of-god","three-pledges","thy-will-be-done-o-lord","to-become-one-of-the-fishers","to-serve-the-master","warnings-from-outer-space","watch-the-switches-in-your-life","we-have-made-covenants-with-the-lord","we-thank-thee-o-god-for-a-prophet","what-is-a-friend","why-do-latter-day-saints-build-temples","why-the-church-of-jesus-christ-of-latter-day-saints"
                }
            },
            {
                new Conference(ConferencePhase.April, 1973),
                new List<string>()
                {
                    "a-second-witness-for-christ","and-always-remember-him","behold-your-little-ones","consider-your-ways","follow-the-leadership-of-the-church","go-and-do-thou-likewise","he-has-sent-his-messenger-to-prepare-the-way","hold-up-your-hands","in-his-strength","magnifying-ones-calling-in-the-priesthood","man-a-child-of-god","now-abideth-faith-hope-and-charity","power-of-evil","priesthood-responsibilities","reaching-the-one","salvation-comes-through-the-church","share-the-unsearchable-riches-of-christ","stand-ye-in-holy-places","strengthen-the-stakes-of-zion","success-a-journey-or-a-destination","the-aaronic-priesthood-mia","the-agency-of-man","the-constant-exercise-of-our-faith","the-continuing-power-of-the-holy-ghost","the-family-influence","the-path-to-eternal-glory","the-rock-of-revelation","the-true-strength-of-the-church","the-worth-of-souls-is-great","this-is-my-gospel","thou-mayest-choose-for-thyself","upon-judeas-plains","watchman-warn-the-wicked","what-is-a-living-prophet","what-manner-of-men-as-i-am","when-i-read-i-am-there","yellow-canaries-with-gray-on-their-wings","youths-opportunity-to-serve"
                }
            },
            {
                new Conference(ConferencePhase.October, 1973),
                new List<string>()
                {
                    "a-fortune-to-share","adversity-and-prayer","become-rich-toward-god","behold-thy-mother","church-welfare-some-fundamentals","closing-remarks","forgiveness-the-ultimate-form-of-love","gods-way-to-eternal-life","happiness-is-having-a-father-who-cares","he-took-him-by-the-hand","i-will-go-and-do-the-things-which-the-lord-hath-commanded","inspiring-music-worthy-thoughts","jesus-christ-our-redeemer","love-and-forgive-one-another","no-greater-honor-the-womans-role","obedience","of-the-world-or-of-the-kingdom","opposition-in-order-to-strengthen-us","our-fundamental-obligation-the-priesthood","our-youth-modern-sons-of-helaman","prepare-ye","president-harold-b-lees-general-priesthood-address","revealed-truths-of-the-gospel","the-gospel-of-jesus-christ-is-the-golden-door","the-need-for-total-commitment","the-path-to-eternal-life","the-rewards-the-blessings-the-promises","the-role-of-fathers","the-witnesses-of-christ","there-is-need-for-repentance","think-on-these-things","thou-shalt-love-thy-wife-with-all-thy-heart","to-be-in-the-world-but-not-of-the-world","understanding-who-we-are-brings-self-respect","we-thank-thee-o-god-for-a-prophet","what-will-a-man-give","which-way-to-shore","you-are-different","you-shall-receive-the-spirit"
                }
            },
            {
                new Conference(ConferencePhase.April, 1974),
                new List<string>()
                {
                    "a-time-of-urgency","aaronic-priesthood-stewardship","be-ye-clean-that-bear-the-vessels-of-the-lord","boys-need-men","build-your-shield-of-faith","chosen-of-the-lord","commitment-to-serve","god-foreordains-his-prophets-and-his-people","guidelines-to-carry-forth-the-work-of-god-in-cleanliness","hanging-on","hearken-unto-the-voice-of-god","his-final-hours","i-will-pour-you-out-a-blessing","inertia","justice-and-mercy","missionary-work-a-major-responsibility","mother-catch-the-vision-of-your-call","parents-teach-your-children","planning-for-a-full-and-abundant-life","prophecy","response-to-a-call","strength-of-the-spirit","testimony","that-the-scriptures-might-be-fulfilled","the-cause-is-just-and-worthy","the-family-a-divine-blessing","the-holy-ghost","the-importance-of-prayer","the-marriage-that-endures","the-paths-jesus-walked","the-people-say-amen","the-power-of-elijah","the-saviors-ministry","the-strength-of-testimony","three-days-in-the-tomb","three-important-questions","touchstone-of-truth","we-believe-all-that-god-has-revealed","what-do-we-hear","what-does-jesus-mean-to-modern-man"
                }
            },
            {
                new Conference(ConferencePhase.October, 1974),
                new List<string>()
                {
                    "a-city-set-upon-a-hill","a-new-aristocracy","a-testimony-of-christ","as-a-beacon-on-a-hill","be-valiant-in-the-fight-of-faith","blessed-are-the-peacemakers","do-not-despair","do-not-procrastinate","eternal-togetherness","for-thy-servant-heareth","gifts-of-the-spirit","god-will-not-be-mocked","good-habits-develop-good-character","how-men-are-saved","integrity","is-there-not-a-cause","making-conferences-turning-points-in-our-lives","my-personal-hall-of-fame","ocean-currents-and-family-influences","our-precious-families","our-responsibility-to-the-transgressor","power-over-satan","the-beatitudes","the-blessings-of-peace","the-davids-and-the-goliaths","the-divine-power-of-repentance","the-law-of-the-fast","the-most-vital-information","the-saviors-program-for-the-care-of-the-aged","to-know-god","transfusion","truth-will-emerge-victorious","we-need-to-continue-in-righteousness","what-after-death","when-thou-art-converted-strengthen-thy-brethren","where-much-is-given-much-is-required","whos-losing","why-is-my-boy-wandering-tonight","why-not-now","your-mission-preparation"
                }
            },
            {
                new Conference(ConferencePhase.April, 1975),
                new List<string>()
                {
                    "a-question-of-free-agency","a-self-inflicted-purging","a-time-for-every-purpose","a-time-to-prepare","a-tribute","an-appeal-to-prospective-elders","anchored-in-testimony","birth","christ-in-america","come-drink-the-living-water","easter-thoughts","faith-the-first-step","faithful-laborers","feed-the-flock","help-for-parents","make-haste-to-obey","my-mother-gained-a-better-son","obedience-consecration-and-sacrifice","one-lord-one-faith-one-baptism","salvation-for-the-dead-a-missionary-activity","scouters-lead-them-to-a-mission","success-is-gauged-by-self-mastery","testimony","the-book-of-mormon-is-the-word-of-god","the-laws-of-god-are-blessings","the-man-of-christ","the-message-of-easter","the-people-who-influence-us","the-road-to-happiness","the-roots-of-mormonism","the-sabbath-day","the-sanctity-of-life","the-symbol-of-christ","the-time-is-now","the-way-home","to-bear-the-priesthood-worthily","today-millions-are-waiting","trust-in-the-lord","using-our-free-agency","we-need-men-of-courage","welfare-services-kimball","welfare-services-romney","why-call-me-lord-lord-and-do-not-the-things-which-i-say","why-do-we-continue-to-tolerate-sin"
                }
            },
            {
                new Conference(ConferencePhase.October, 1975),
                new List<string>()
                {
                    "a-call-to-arms","a-message-to-the-world","a-prophets-faith","according-to-the-covenants","americas-destiny","an-overview-of-church-welfare-services","but-watchman-what-of-the-night","covenants-and-blessings","do-it","faith-and-works-in-the-far-east","family-home-evening","family-research","for-the-time-will-come-when-they-will-not-endure-sound-doctrine","for-they-loved-the-praise-of-men-more-than-the-praise-of-god","hear-ye-him","immanuel-god-with-us","let-all-thy-doings-be-unto-the-lord","love-takes-time","my-gratitude","my-heritage-is-choice","oh-beautiful-for-patriot-dream","once-or-twice-in-a-thousand-years","opposing-evil","prepare-for-honorable-employment","prophets-and-prophecy","relief-societys-role-in-welfare-services","spoken-from-their-hearts","success-stories","the-developing-welfare-services-department","the-faith-of-a-child","the-importance-of-reputation","the-keys-of-the-kingdom","the-language-of-the-spirit","the-laws-of-god","the-power-of-the-priesthood","the-privilege-of-holding-the-priesthood","the-redemption-of-the-dead","the-tabernacle","the-time-to-labor-is-now","the-vision-of-the-aaronic-priesthood","the-welfare-production-distribution-department","the-worlds-greatest-need","there-is-still-much-to-do","to-cleanse-our-souls","to-make-a-people-prepared-for-the-lord","we-are-sent-for-the-last-time","we-can-t-do-it-alone","welfare-services","why-can-t-we","you-too-must-know"
                }
            },
            {
                new Conference(ConferencePhase.April, 1976),
                new List<string>()
                {
                    "a-living-prophet","a-missionary-opportunity","an-honest-man-gods-noblest-work","are-we-following-christs-pattern","are-you-a-member-missionary","are-you-taking-your-priesthood-for-granted","as-a-man-soweth","boys-need-heroes-close-by","but-they-were-in-one","church-welfare-services-basic-principles","easter-thoughts","family-communications","family-preparedness","food-storage","he-is-the-son-of-god","hopeless-dawn-joyful-morning","if-they-will-but-serve-the-god-of-the-land","jesus-of-nazareth-savior-and-king","joseph-smith-the-mighty-prophet-of-the-restoration","learn-obedience-and-service","on-accepting-the-call","priesthood-authority-and-power","relationships","seek-not-for-riches-but-for-wisdom","seeking-eternal-riches","shout-it-from-the-rooftops","spiritual-crocodiles","teach-lds-women-self-sufficiency","that-we-may-be-one","the-blessing-of-building-a-temple","the-book-of-mormon","the-church-and-the-family-in-welfare-services","the-church-employment-system","the-constitution-a-glorious-standard","the-lamanites-must-rise-in-majesty-and-power","the-leaves-are-commencing-to-show-on-the-fig-tree","the-matter-of-personal-testimony","the-message-of-elijah","the-right-to-choose","the-still-small-voice","the-stone-cut-without-hands","the-value-of-people","the-way-of-life","the-word-of-wisdom","there-am-i-in-the-midst-of-them","these-four-things","value-of-the-holy-scriptures","who-is-jesus","you-are-your-greatest-treasure"
                }
            },
            {
                new Conference(ConferencePhase.October, 1976),
                new List<string>()
                {
                    "a-gospel-of-conversion","a-personal-relationship-with-the-savior","a-program-for-man","a-report-and-a-challenge","a-report-on-the-churchs-participation-in-americas-bicentennial-celebration","acquiring-and-managing-production-projects","dikes-versus-living-water","everything-to-gain-nothing-to-lose","extending-missionary-service","families-are-forever","follow-it","go-and-do-the-work","i-am-the-vine-ye-are-the-branches","i-have-gained","i-will-never-be-the-same-again","in-mine-own-way","loving-one-another","more-joy-and-rejoicing","notwithstanding-my-weakness","our-goal-is-perfection","our-own-liahona","our-priceless-heritage","parenthood","perfecting-the-saints","principles-of-welfare","proper-self-management","ready-to-work-long-hours","search-the-scriptures","she-is-not-afraid-of-the-snow-for-her-household","teachings-of-the-apostle-paul","the-dead-who-die-in-the-lord","the-greatest-thing-in-my-life","the-impact-teacher","the-living-prophet","the-lord-offers-everyone-a-way-back-from-sin","the-lords-support-system","the-making-of-a-missionary","the-purpose-of-conferences","the-reconstitution-of-the-first-quorum-of-the-seventy","the-savor-of-men","the-simplicity-in-christ","the-spirit-of-missionary-work","the-temptations-of-christ","there-is-the-light","to-die-well","we-are-a-covenant-making-people","we-believe-in-being-honest","welfare-services-essentials-the-bishops-storehouse","which-road-will-you-travel","you-never-know-who-you-may-save","your-gift-from-god"
                }
            },
            {
                new Conference(ConferencePhase.April, 1977),
                new List<string>()
                {
                    "a-call-to-action","a-silver-lining","a-thousand-witnesses","but-be-ye-doers-of-the-word","come-know-the-lord-jesus","come-let-israel-build-zion","did-not-our-heart-burn-within-us","do-unto-others","follow-the-living-prophet","god-moves-in-a-mysterious-way-his-wonders-to-perform","gratitude","integrity","joseph-the-seer","lengthening-your-stride-as-a-missionary","ministering-to-needs-through-lds-social-services","ministering-to-needs-through-the-lords-storehouse-system","neither-cryptic-nor-hidden","our-great-potential","prayer","prepare-now-for-your-mission","presentation-of-scouting-award","rendering-assistance-in-the-lords-way","revelation-the-word-of-the-lord-to-his-prophets","the-beatitudes-and-our-perfection","the-daily-portion-of-love","the-greatest-brotherhood","the-light-of-christ","the-living-christ","the-lord-expects-his-saints-to-follow-the-commandments","the-mediator","the-power-of-plainness","the-purpose-of-church-welfare-services","the-role-of-the-stake-bishops-council-in-welfare-services","the-validity-of-the-gospel","thoughts-on-the-sacrament","we-beheld-his-glory","what-constitutes-the-true-church","your-jericho-road"
                }
            },
            {
                new Conference(ConferencePhase.October, 1977),
                new List<string>()
                {
                    "a-message-to-the-rising-generation","a-special-moment-in-church-history","a-vision-of-the-law-of-the-fast","father-your-role-your-responsibility","hallowed-be-thy-name","it-was-a-miracle","jesus-the-christ","latter-day-samaritans","letter-to-a-returned-missionary","obeying-the-right-voice","rated-a","sacrifice-missionary-style","seeing-the-five-a-s","service-saves","she-stretcheth-out-her-hand-to-the-poor","the-balm-of-gilead","the-blessings-of-righteous-obedience","the-enriching-of-marriage","the-fathers-duty-to-foster-the-welfare-of-his-family","the-foundations-of-righteousness","the-light-of-the-gospel","the-power-of-forgiveness","the-role-of-bishops-in-welfare-services","the-safety-of-the-gospel-law","the-ten-blessings-of-the-priesthood","the-things-of-god-and-man","the-tragic-cycle","the-way-of-the-lord","they-didn-t-give-up","three-things-to-share","trust-in-the-lord","we-have-been-there-all-the-time","welfare-responsibilities-of-the-priesthood-quorums","welfare-services-the-gospel-in-action","why-me-o-lord","young-women-real-guardians"
                }
            },
            {
                new Conference(ConferencePhase.April, 1978),
                new List<string>()
                {
                    "a-haven-of-love","be-not-faithless","becoming-the-pure-in-heart","being-anxiously-engaged","bind-on-thy-sandals","decision","everything-dear","evidence-of-things-not-seen","grieve-not-the-holy-spirit-lest-we-lose-it","i-did-not-reach-this-place-by-myself","in-the-time-of-old-age","listen-to-the-prophets","making-your-marriage-successful","may-the-kingdom-of-god-go-forth","no-time-for-contention","not-my-will-but-thine","prayer-and-revelation","priesthood-responsibilities","response-to-the-call","revelation","seek-out-your-spiritual-leader","solving-emotional-problems-in-the-lords-own-way","staying-unspotted-from-the-world","strengthening-the-family-the-basic-unit-of-the-church","the-morning-breaks-the-shadows-flee","the-poetry-of-success","the-prayer-of-faith","the-primary-enriches-the-lives-of-children","the-royal-law-of-love","the-second-coming-of-christ","the-storehouse-resource-system","the-true-way-of-life-and-salvation","the-women-of-god","trust-in-the-lord","we-are-his-stewards","welfare-services-begins-with-you","what-is-truth","what-would-the-savior-have-me-do","worthy-of-proper-recommendation","ye-shall-know-the-truth"
                }
            },
            {
                new Conference(ConferencePhase.October, 1978),
                new List<string>()
                {
                    "a-basis-for-faith-in-the-living-god","a-disciple-of-christ","an-eternal-hope-in-christ","attending-to-personal-and-family-preparedness","be-one-with-the-prophet","behold-your-little-ones","caring-for-the-poor-a-covenantal-obligation","come-home-felila","come-listen-to-a-prophets-voice","faith-courage-and-making-choices","fundamental-principles-to-ponder-and-live","good-health-a-key-to-joyous-living","he-hath-showed-thee-o-man-what-is-good","hold-fast-to-the-iron-rod","home-teaching-a-sacred-calling","if-we-want-to-go-up-we-have-to-get-on","let-there-be-no-misunderstanding","let-your-light-so-shine","look-to-god-and-live","never-be-weary-of-good-works","ours-is-a-shared-ancestry","privileges-and-responsibilities-of-sisters","profiles-of-faith","response-to-the-call","spiritual-development","successful-welfare-stewardship","the-blessing-of-church-interviews","the-cs-of-spirituality","the-fruit-of-our-welfare-services-labors","the-gift-of-love","the-gospel-makes-people-happy","the-joy-of-serving-a-mission","the-last-words-of-moroni","the-relief-society","the-remarkable-example-of-the-bermejillo-mexico-branch","the-stake-presidents-role-in-welfare-services","the-worth-of-souls","thou-shalt-receive-revelation","true-religion","what-are-the-blessings-of-a-mission-can-ye-tell","who-will-forfeit-the-harvest","womens-greatest-challenge","worthy-of-all-acceptation"
                }
            },
            {
                new Conference(ConferencePhase.April, 1979),
                new List<string>()
                {
                    "a-personal-commitment","and-peter-went-out-and-wept-bitterly","applying-the-principles-of-welfare-services","because-i-have-a-father","church-government-through-councils","developing-spirituality","developing-temporal-plans-and-priorities","feed-my-sheep","following-christ-to-victory","fortify-your-homes-against-evil","fundamental-welfare-services","he-means-me","judge-not-according-to-the-appearance","let-us-move-forward-and-upward","new-emphasis-on-church-councils","personal-and-family-financial-preparedness","preparing-for-service-in-the-church","put-on-the-whole-armor-of-god","roadblocks-to-progress","signs-of-the-true-church","stand-independent-above-all-other-creatures","the-army-of-the-lord","the-heritage-of-royal-families","the-home-as-an-educational-institution","the-kingdom-of-god","the-need-for-love","the-refiners-fire","the-spirit-giveth-life","this-is-a-day-of-sacrifice","to-those-searching-for-happiness","trust-in-the-lord","we-the-church-of-jesus-christ-of-latter-day-saints","what-think-ye-of-christ-whom-say-ye-that-i-am"
                }
            },
            {
                new Conference(ConferencePhase.October, 1979),
                new List<string>()
                {
                    "a-witness-and-a-warning","after-much-tribulation-come-the-blessings","an-angel-from-on-high-the-long-long-silence-broke","blessing-the-one","commandments-to-live-by","constancy-amid-change","establishing-the-church-welfare-services-missionaries-are-an-important-resource","faith-in-the-lord-jesus-christ","give-me-this-mountain","happiness-now-and-forever","joseph-smith-the-prophet","language-a-divine-way-of-communicating","maintaining-spirituality","making-the-right-decisions","o-america-america","our-mighty-errand","our-sisters-in-the-church","pornography-the-deadly-carrier","prayer-to-our-heavenly-father","prayers-and-answers","priesthood-administration-of-welfare-services","progress-through-change","reading-the-scriptures","send-missionaries-from-every-nation","teaching-our-little-women","the-administration-of-the-church","the-contributions-of-the-prophet-joseph-smith","the-gift-of-the-holy-ghost","the-governing-ones","the-mystery-of-mormonism","the-relief-society-role-in-priesthood-councils","the-role-of-a-bishop-in-the-church-welfare-program","the-role-of-righteous-women","the-voice-of-the-lord-is-unto-all-people","therefore-i-was-taught","we-need-a-listening-ear","we-will-go-with-our-young-and-with-our-old","women-for-the-latter-day"
                }
            },
            {
                new Conference(ConferencePhase.April, 1980),
                new List<string>()
                {
                    "a-deep-commitment-to-the-principles-of-welfare-service","a-marvelous-work-and-a-wonder","a-tribute-to-the-rank-and-file-of-the-church","an-example-of-what-welfare-services-can-do","celestial-marriages-and-eternal-families","church-welfare-temporal-service-in-a-spiritual-setting","communion-with-the-holy-spirit","do-we-all-believe-in-the-same-god","eternal-links-that-bind","god-will-have-a-tried-people","he-is-not-here-he-is-risen","introduction-to-the-proclamation","let-us-not-weary-in-well-doing","nauvoo-a-demonstration-of-faith","no-unhallowed-hand-can-stop-the-work","preparing-the-way","priesthood-councils-key-to-meeting-temporal-and-spiritual-needs","remarks-and-dedication-of-the-fayette-new-york-buildings","salt-of-the-earth-savor-of-men-and-saviors-of-men","seek-the-spirit","self-accountability-and-human-progress","teaching-by-the-spirit","the-book-of-mormon","the-coming-tests-and-trials-and-glory","the-gospel-restored","the-prophet-and-the-prison","the-scriptures-speak","time-out","welfare-principles-in-relief-society","welfare-services-past-present-and-future","what-hath-god-wrought-through-his-servant-joseph","where-do-we-stand","willing-to-receive","writing-your-personal-and-family-history","you-can-be-the-voice"
                }
            },
            {
                new Conference(ConferencePhase.October, 1980),
                new List<string>()
                {
                    "a-testimony","acquaint-thyself-with-him-and-be-at-peace","adam-the-archangel","adversity-and-you","decide-to-decide","do-not-weary-by-the-way","families-can-be-eternal","feed-my-sheep","follow-joyously","for-whatsoever-a-man-soweth-that-shall-he-also-reap","forgive-them-i-pray-thee","is-any-thing-too-hard-for-the-lord","know-the-shepherd","learn-then-teach","let-every-man-learn-his-duty","ministering-to-the-needs-of-members","miracles-among-the-lamanites","motherhood-and-the-family","of-you-it-is-required-to-forgive","organize-yourselves","our-thirtieth-anniversary-as-latter-day-saints","prepare-every-needful-thing","prepare-for-the-days-of-tribulation","purify-our-minds-and-spirits","repentance","seven-events-of-great-consequence","singleness-how-relief-society-can-help","the-bishop-center-stage-in-welfare","the-blessing-of-a-testimony","the-bond-of-charity","the-choice","the-circle-of-sisters","the-doctrines-of-the-kingdom","the-house-of-the-lord","the-household-of-faith","the-keys-of-the-kingdom","the-law-of-tithing","the-lord-god-of-the-restoration","the-net-gathers-of-every-kind","the-oath-and-covenant-which-belongeth-to-the-priesthood","the-saviors-touch","these-i-will-make-my-leaders","to-the-young-men-of-the-church","welfare-services-the-saviors-program"
                }
            },
            {
                new Conference(ConferencePhase.April, 1981),
                new List<string>()
                {
                    "a-report-of-my-stewardship","blessings-in-self-reliance","building-bridges-to-faith","call-of-the-prophets","fast-offerings-fulfilling-our-responsibility-to-others","follow-the-fundamentals","gospel-covenants","gracias","great-things-required-of-their-fathers","he-is-there","in-saving-others-we-save-ourselves","life-a-great-proving-ground","light-and-truth","love-one-another","marriage","moral-values-and-rewards","no-man-shall-add-to-or-take-away","obedience-full-obedience","our-responsibility-to-care-for-our-own","providing-for-our-needs","reach-for-the-stars","reach-out-to-our-fathers-children","rendering-service-to-others","the-basic-principles-of-church-welfare","the-dignity-of-self","the-joseph-smith-iii-document-and-the-keys-of-the-kingdom","the-long-line-of-the-lonely","the-need-to-teach-personal-and-family-preparedness","the-responsibility-of-young-aaronic-priesthood-bearers","the-restoration-of-israel-to-the-lands-of-their-inheritance","turning-the-hearts","upon-this-rock","we-are-called-to-spread-the-light","we-are-on-the-lords-errand","we-serve-that-which-we-love"
                }
            },
            {
                new Conference(ConferencePhase.October, 1981),
                new List<string>()
                {
                    "a-safe-place-for-marriages-and-families","an-opportunity-for-continual-learning","be-ye-prepared","being-strengthened-through-service","charity-never-faileth","conference-time","examples-from-the-life-of-a-prophet","except-a-man-be-born-again","faith-the-essence-of-true-religion","finding-joy-by-serving-others","follow-the-prophets","four-bs-for-boys","give-with-wisdom-that-they-may-receive-with-dignity","he-is-risen","joseph-smith-prophet-to-our-generation","living-welfare-principles","love-extends-beyond-convenience","my-sheep-hear-my-voice","my-specialty-is-mercy","o-divine-redeemer","opposition-to-the-work-of-god","people-to-people","relief-society-in-times-of-transition","relief-society-in-welfare","remember-who-you-are","sanctification-through-missionary-service","teach-the-why","the-aaronic-priesthood","the-expanding-inheritance-from-joseph-smith","the-honored-place-of-woman","the-light-of-the-gospel","the-little-things-and-eternal-life","the-ministry-of-the-aaronic-priesthood-holder","the-perfect-law-of-liberty","the-plan-for-happiness-and-exaltation","the-strength-of-the-kingdom-is-within","to-follow-or-not-that-is-the-question","when-ye-are-prepared-ye-shall-not-fear","who-hath-believed-our-report"
                }
            },
            {
                new Conference(ConferencePhase.April, 1982),
                new List<string>()
                {
                    "a-brother-offended","a-lasting-marriage","an-invitation-to-grow","beginning-again","employment-challenges-in-the-1980s","even-as-i-am","five-million-members-a-milestone-and-not-a-summit","gods-love-for-us-transcends-our-transgressions","hearts-so-similar","her-children-arise-up-and-call-her-blessed","integrity-the-mother-of-many-virtues","jesus-is-our-savior","let-us-go-up-to-the-house-of-god","let-us-improve-ourselves","love-is-the-power-that-will-cure-the-family","pondering-strengthens-the-spiritual-life","priesthood-activation","priesthood","reach-for-joy","remember-the-mission-of-the-church","sailing-safely-the-seas-of-life","spiritual-guides-for-teachers-of-righteousness","the-doctrine-of-the-priesthood","the-first-and-the-last-words","the-future-history-of-the-church","the-gospel-the-foundation-for-our-career","the-lord-is-at-the-helm","the-power-of-family-prayer","the-resurrection-of-jesus","the-value-of-work","this-is-no-harm","tithing-an-opportunity-to-prove-our-faithfulness","true-greatness","valiant-in-the-testimony-of-jesus","we-believe-in-being-honest","what-temples-are-for","what-the-gospel-teaches","work-and-welfare-a-historical-perspective"
                }
            },
            {
                new Conference(ConferencePhase.October, 1982),
                new List<string>()
                {
                    "activating-young-men-of-the-aaronic-priesthood","application-of-welfare-principles-in-the-home-a-key-to-many-family-problems","be-a-friend-a-servant-a-son-of-the-savior","be-of-good-cheer","behold-my-beloved-son-in-whom-i-am-well-pleased","believers-and-doers","commitment-to-god","faith-the-force-of-life","for-a-bishop-must-be-blameless","fundamentals-of-enduring-family-relationships","gratitude-and-thanksgiving","how-we-promote-activation","however-faint-the-light-may-glow","lds-hymns-worshiping-with-song","let-us-do-as-we-have-been-counseled","look-to-god","love-all","my-soul-delighteth-in-the-scriptures","preparation-for-tomorrow","prepare-the-heart-of-your-son","pure-religion","reach-out-in-love-and-kindness","revitalizing-aaronic-priesthood-quorums","run-boy-run","scriptures","the-blessings-of-family-work-projects","the-blessings-we-receive-as-we-meet-the-challenges-of-economic-stress","the-celestial-nature-of-self-reliance","the-lord-expects-righteousness","the-meaning-of-maturity","the-pearl-of-great-price","the-power-of-the-priesthood","the-priesthood-of-aaron","the-seven-christs","what-this-work-is-all-about"
                }
            },
            {
                new Conference(ConferencePhase.April, 1983),
                new List<string>()
                {
                    "a-call-to-the-priesthood-feed-my-sheep","a-principle-with-a-promise","a-royal-generation","agency-and-control","anonymous","climbing-to-higher-spirituality","creator-and-savior","enriching-family-life","evidences-of-the-resurrection","fear-not-to-do-good","finding-ones-identity","he-slumbers-not-nor-sleeps","muddy-feet-and-white-shirts","overpowering-the-goliaths-in-our-lives","profanity-and-swearing","receiving-a-prophet","repentance","shine-as-lights-in-the-world","straightway","teaching-no-greater-call","that-ye-may-have-roots-and-branches","the-gospel-of-jesus-christ-and-basic-needs-of-people","the-keys-of-the-kingdom","the-sacrament","to-forgive-is-divine","train-up-a-child","unity","valiance-in-the-drama-of-life","within-the-clasp-of-your-arms"
                }
            },
            {
                new Conference(ConferencePhase.October, 1983),
                new List<string>()
                {
                    "a-season-for-strength","agency-and-accountability","agency-and-love","be-a-peacemaker","be-not-deceived","become-a-star-thrower","called-as-if-he-heard-a-voice-from-heaven","except-ye-are-one","friend-or-foe","god-grant-us-faith","honour-thy-father-and-thy-mother","how-do-you-know","jesus-christ-our-savior-and-redeemer","joseph-the-seer","labels","let-us-go-forward","live-up-to-your-inheritance","our-father-which-art-in-heaven","our-responsibility-to-take-the-gospel-to-the-ends-of-the-earth","parent-child-interviews","parents-concern-for-children","prepare-to-teach-his-children","removing-the-poison-of-an-unforgiving-spirit","the-angel-moroni-came","the-blessings-of-missionary-service","the-house-of-the-lord","the-keystone-of-our-religion","the-mystery-of-life","the-power-to-make-a-difference","the-word-is-commitment","what-manner-of-men-ought-we-to-be","what-think-ye-of-the-book-of-mormon","your-sorrow-shall-be-turned-to-joy"
                }
            },
            {
                new Conference(ConferencePhase.April, 1984),
                new List<string>()
                {
                    "a-generation-prepared-to-make-wise-choices","building-your-eternal-home","call-to-the-holy-apostleship","choose-the-good-part","coming-through-the-mists","counsel-to-the-saints","count-your-blessings","covenants-ordinances-and-service","feed-my-sheep","go-ye-therefore-and-teach-all-nations","home-and-family-a-divine-eternal-pattern","i-love-the-sisters-of-the-church","jesus-the-christ-the-words-and-their-meaning","marriage-and-divorce","missions-only-you-can-decide","our-commission-to-take-the-gospel-to-all-the-world","patterns-of-prayer","restoring-the-lost-sheep","small-acts-lead-to-great-consequences","special-witnesses-for-christ","the-great-plan-of-the-eternal-god","the-magnificent-vision-near-palmyra","the-miracle-made-possible-by-faith","the-pharisee-and-the-publican","the-practice-of-truth","the-simplicity-of-gospel-truths","the-sure-sound-of-the-trumpet","upheld-by-the-prayers-of-the-church","warmed-by-the-fires-of-their-lives","whos-on-the-lords-team","youth-of-the-noble-birthright"
                }
            },
            {
                new Conference(ConferencePhase.October, 1984),
                new List<string>()
                {
                    "a-new-witness-for-christ","and-why-call-ye-me-lord-lord-and-do-not-the-things-which-i-say","by-their-fruits-ye-shall-know-them","coordination-and-cooperation","eternal-marriage","he-returned-speedily","if-thou-art-faithful","if-thou-endure-it-well","keeping-the-covenants-we-make-at-baptism","learning-our-fathers-will","live-the-gospel","master-the-tempest-is-raging","out-of-obscurity","personal-morality","prepare-for-a-mission","protect-the-spiritual-power-line","service-in-the-church","spiritual-power","striving-together-transforming-our-beliefs-into-action","the-aaronic-priesthood-pathway","the-banner-of-the-lord","the-caravan-moves-on","the-cornerstones-of-our-faith","the-faith-of-our-people","the-good-and-faithful-servants","the-gospel-and-the-church","the-joy-of-service","the-joy-of-the-penetrating-light","the-pattern-of-our-parentage","the-power-of-keeping-the-sabbath-day-holy","the-works-of-god","when-i-was-called-as-a-scoutmaster","why-do-we-serve","write-down-a-date","young-women-striving-together"
                }
            },
            {
                new Conference(ConferencePhase.April, 1985),
                new List<string>()
                {
                    "agency-and-accountability","born-of-goodly-parents","christ-our-passover","confidence-in-the-lord","ears-to-hear","from-such-turn-away","god-has-a-work-for-us-to-do","he-is-in-charge","hold-up-your-light","look-for-the-beautiful","our-responsibility-to-share-the-gospel","prepare-to-serve","preparing-yourselves-for-missionary-service","pursuing-excellence","reverence-for-life","selflessness-a-pattern-for-happiness","set-some-personal-goals","spencer-w-kimball-a-true-disciple-of-christ","taking-upon-us-the-name-of-jesus-christ","the-answers-will-come","the-invitation-of-the-master","the-joy-of-service","the-mantle-of-a-bishop","the-purifying-power-of-gethsemane","the-resurrected-christ","the-resurrection","the-spirit-giveth-life","the-spirit-of-the-gathering","the-victory-over-death","this-is-the-work-of-the-lord","to-please-our-heavenly-father","willing-to-submit"
                }
            },
            {
                new Conference(ConferencePhase.October, 1985),
                new List<string>()
                {
                    "adventures-of-the-spirit","as-i-have-loved-you","born-of-god","by-their-fruits-ye-shall-know-them","can-there-any-good-thing-come-out-of-nazareth","draw-near-to-him-in-prayer","draw-near-unto-me-through-obedience","draw-near-unto-me","fast-day","i-confer-the-priesthood-of-aaron","in-response-to-the-call","joined-together-in-love-and-faith","joseph-smith-the-chosen-instrument","lessons-from-the-atonement-that-help-us-to-endure-to-the-end","let-mercy-temper-justice","let-us-move-this-work-forward","peace-a-triumph-of-principles","premortality-a-glorious-reality","questions-and-answers","rejoice-in-this-great-era-of-temple-building","self-mastery","spirituality","ten-gifts-from-the-lord","the-abundant-life","the-gospel-lifeline","the-gospel-of-love","the-gospel","the-heavens-declare-the-glory-of-god","the-holy-scriptures-letters-from-home","the-oath-and-covenant-of-the-priesthood","the-only-true-church","those-who-love-jesus","whats-the-difference","worthy-fathers-worthy-sons"
                }
            },
            {
                new Conference(ConferencePhase.April, 1986),
                new List<string>()
                {
                    "a-prophet-chosen-of-the-lord","a-provident-plan-a-precious-promise","a-sacred-responsibility","an-apostles-witness-of-the-resurrection","be-of-good-cheer","called-and-prepared-from-the-foundation-of-the-world","cleansing-the-inner-vessel","come-and-partake","happiness","in-the-lords-own-way","principles-and-programs","reverent-and-clean","sixteen-years-as-a-witness","the-call-of-duty","the-greatest-news-of-all-times-is-that-jesus-lives","the-kingdom-rolls-forth-in-south-america","the-law-of-the-fast","the-power-of-the-word","the-question-of-a-mission","the-responsibility-for-welfare-rests-with-me-and-my-family","the-things-of-my-soul","they-taught-and-did-minister-one-to-another","to-the-youth-of-the-noble-birthright","we-love-you-please-come-back","welfare-principles-to-guide-our-lives-an-eternal-plan-for-the-welfare-of-mens-souls"
                }
            },
            {
                new Conference(ConferencePhase.October, 1986),
                new List<string>()
                {
                    "a-father-speaks","a-time-for-hope","brothers-keeper","come-back-to-the-lord","courage-counts","developing-faith","god-will-yet-reveal","godly-characteristics-of-the-master","happiness-and-joy-in-temple-work","hope-in-christ","i-will-look-unto-the-lord","joy-cometh-in-the-morning","little-children","missionary-work-is-the-lifeblood-of-the-church","my-son-and-yours-each-a-remarkable-one","presidents-of-the-church","pulling-in-the-gospel-net","shake-off-the-chains-with-which-ye-are-bound","spiritual-crevasses","the-book-of-mormon-keystone-of-our-religion","the-father-son-and-holy-ghost","the-gift-of-modern-revelation","the-joy-of-honest-labor","the-light-of-hope","the-lords-touchstone","the-spark-of-faith","the-war-we-are-winning","to-the-young-women-of-the-church","touching-the-hearts-of-less-active-members","unwanted-messages","we-proclaim-the-gospel","your-patriarchal-blessing-a-liahona-of-light"
                }
            },
            {
                new Conference(ConferencePhase.April, 1987),
                new List<string>()
                {
                    "am-i-a-living-member","by-faith-and-hope-all-things-are-fulfilled","covenants","i-am-an-adult-now","keeping-lifes-demands-in-balance","life-after-life","looking-to-the-savior","my-neighbor-my-brother","no-shortcuts","overcome-even-as-i-also-overcame","patience-a-key-to-happiness","priesthood-blessings","reverence-and-morality","some-have-compassion-making-a-difference","spiritual-security","tears-trials-trust-testimony","the-blessings-of-being-unified","the-book-of-mormon-and-the-doctrine-and-covenants","the-book-of-mormons-witness-of-jesus-christ","the-lengthened-shadow-of-the-hand-of-god","the-saviors-visit-to-america","the-will-within","to-the-home-teachers-of-the-church","united-in-building-the-kingdom-of-god","what-it-means-to-be-a-saint","will-i-be-happy"
                }
            },
            {
                new Conference(ConferencePhase.October, 1987),
                new List<string>()
                {
                    "a-champion-of-youth","a-doorway-called-love","a-meaningful-celebration","balm-of-gilead","called-to-serve","come-unto-christ","ethics-and-honesty","finding-joy-in-life","follow-the-brethren","i-will-go-and-do","in-the-service-of-the-lord","keys-of-the-priesthood","lessons-from-eve","looking-beyond-the-mark","lord-increase-our-faith","missionary-memories","never-give-up","opportunities-to-serve","our-divine-constitution","overcoming-challenges-along-lifes-way","sacrifice-and-self-sufficiency","selfless-service","strengthening-the-family","take-not-the-name-of-god-in-vain","the-dawning-of-a-new-day-in-africa","the-great-imitator","the-light-and-life-of-the-world","the-opening-and-closing-of-doors","there-are-many-gifts","they-re-not-really-happy","to-the-fathers-in-israel","yet-thou-art-there"
                }
            },
            {
                new Conference(ConferencePhase.April, 1988),
                new List<string>()
                {
                    "always-remember-him","an-invitation-to-exaltation","assurance-that-comes-from-knowing","atonement-agency-accountability","because-i-pray-for-you","because-of-your-steadiness","come-unto-christ-and-be-perfected-in-him","daughter-of-god","for-i-will-lead-you-along","gods-love-for-his-children","happiness-through-service","he-is-risen","in-the-world","our-lord-and-savior","seek-the-blessings-of-the-church","shepherds-of-israel","solutions-from-the-scriptures","teach-children-the-gospel","the-aaronic-priesthood-a-gift-from-god","the-empty-tomb-bore-testimony","the-great-commandment-love-the-lord","the-highest-place-of-honor","to-help-a-loved-one-in-need","to-the-single-adult-brethren-of-the-church","what-think-ye-of-christ","while-they-are-waiting","with-god-nothing-shall-be-impossible","without-guile","you-make-a-difference"
                }
            },
            {
                new Conference(ConferencePhase.October, 1988),
                new List<string>()
                {
                    "a-call-to-serve","a-more-excellent-way","a-willing-heart","addiction-or-freedom","answer-me","becoming-a-prepared-people","blessed-from-on-high","children-at-peace","choose-the-church","christlike-communications","flooding-the-earth-with-the-book-of-mormon","funerals-a-time-for-reverence","goal-beyond-victory","hallmarks-of-a-happy-home","i-testify","i-will-follow-gods-plan-for-me","inviting-others-to-come-unto-christ","making-righteous-choices-at-the-crossroads-of-life","stand-for-truth-and-righteousness","the-hand-of-fellowship","the-healing-power-of-christ","the-measure-of-our-hearts","the-priesthood-of-god","the-quality-of-eternal-life","the-royal-law-of-love","the-soil-and-roots-of-testimony","the-supernal-gift-of-the-atonement","to-the-bishops-of-the-church","to-the-single-adult-sisters-of-the-church","train-up-a-child","true-friends-that-lift","we-have-a-work-to-do","what-think-ye-of-christ","what-went-ye-out-to-see"
                }
            },
            {
                new Conference(ConferencePhase.April, 1989),
                new List<string>()
                {
                    "adversity-and-the-divine-purpose-of-mortality","alternate-voices","beware-of-pride","follow-the-prophet","go-for-it","irony-the-crust-on-the-bread-of-adversity","let-love-be-the-lodestar-of-your-life","lord-when-saw-we-thee-an-hungred","magnify-your-calling","making-points-for-righteousness","now-is-the-time","on-being-worthy","our-kindred-family-expression-of-eternal-love","proclaim-my-gospel-from-land-to-land","seeds-of-renewal","thanks-be-to-god","the-beauty-and-importance-of-the-sacrament","the-canker-of-contention","the-effects-of-television","the-gift-of-the-holy-ghost-a-sure-compass","the-god-that-doest-wonders","the-way-to-perfection","to-the-children-of-the-church","to-young-women-and-men","trust-in-the-lord","university-for-eternal-life"
                }
            },
            {
                new Conference(ConferencePhase.October, 1989),
                new List<string>()
                {
                    "a-lifetime-of-learning","a-word-of-benediction","an-ensign-to-the-nations","an-eye-single-to-the-glory-of-god","chastity-the-source-of-true-manhood","continuous-revelation","duties-rewards-and-risks","follow-him","good-memories-are-real-blessings","he-loved-them-unto-the-end","identity-of-a-young-woman","keep-the-faith","learning-to-recognize-answers-to-prayer","look-to-the-savior","love","modern-pioneers","murmur-not","overcoming-adversity","remember-him","remembrance-and-gratitude","revelation-in-a-changing-world","rise-to-the-stature-of-the-divine-within-you","running-your-marathon","stalwart-and-brave-we-stand","the-golden-thread-of-choice","the-peaceable-followers-of-christ","the-sacrament-and-the-sacrifice","the-scourge-of-illicit-drugs","the-service-that-counts","the-summer-of-the-lambs","the-value-of-preparation","to-the-elderly-in-the-church","winding-up-our-spiritual-clocks","windows","woman-of-infinite-worth"
                }
            },
            {
                new Conference(ConferencePhase.April, 1990),
                new List<string>()
                {
                    "a-latter-day-samaritan","a-little-child-shall-lead-them","blessed-are-the-merciful","choose-you-this-day","conference-is-here","endure-it-well","family-traditions","filling-the-whole-earth","finding-the-way-back","gratitude-as-a-saving-principle","home-first","i-will-go-and-do","instruments-to-accomplish-his-purposes","keeping-the-temple-holy","my-brothers-keeper","neither-boast-of-faith-nor-of-mighty-works","one-small-step-for-a-man-one-giant-leap-for-mankind","personal-integrity","preparing-the-heart","rise-to-a-larger-vision-of-the-work","sacred-resolves","small-and-simple-things","some-scriptural-lessons-on-leadership","standing-as-witnesses-of-god","teach-them-correct-principles","teachings-of-a-loving-father","the-aaronic-priesthood-return-with-honor","the-greatest-joy","the-library-of-the-lord","the-lords-way","the-motorcycle-ride","the-resurrection","the-spirituality-of-service","thus-shall-my-church-be-called","who-is-a-true-friend","world-peace","ye-have-done-it-unto-me"
                }
            },
            {
                new Conference(ConferencePhase.October, 1990),
                new List<string>()
                {
                    "a-pattern-in-all-things","a-thousand-times","an-eternal-key","changing-channels","choices","come-unto-me","covenants","crickets-can-be-destroyed-through-spirituality","days-never-to-be-forgotten","draw-strength-from-the-book-of-mormon","follow-the-prophet","follow-the-prophets","god-be-with-you-till-we-meet-again","happiness-is-homemade","hour-of-conversion","in-all-things-give-thanks","in-counsellors-there-is-safety","kindness-a-part-of-gods-plan","mormon-should-mean-more-good","no-more-strangers-and-foreigners","purity-precedes-power","put-off-the-natural-man-and-come-off-conqueror","redemption-the-harvest-of-love","serve-god-acceptably-with-reverence-and-godly-fear","temples-and-work-therein","that-we-may-touch-heaven","the-greatest-challenge-in-the-world-good-parenting","the-lighthouse-of-the-lord","the-many-witnesses-of-jesus-christ-and-his-work","the-resurrection","the-straight-and-narrow-way","the-value-of-a-testimony","the-word-of-wisdom","these-things-are-manifested-unto-us-plainly","this-work-will-go-forward","what-is-truth","witnesses-of-christ"
                }
            },
            {
                new Conference(ConferencePhase.April, 1991),
                new List<string>()
                {
                    "a-crown-of-thorns-a-crown-of-glory","a-pattern-of-righteousness","a-royal-priesthood","a-voice-of-gladness","before-i-build-a-wall","beware-lest-thou-forget-the-lord","called-to-serve","change","his-latter-day-kingdom-has-been-established","honour-thy-father-and-thy-mother","lest-ye-be-wearied-and-faint-in-your-minds","linking-the-family-of-man","listen-to-learn","making-the-right-decisions","never-alone","peace-within","peace","prophets","redemption-of-the-dead","repentance","sunday-worship-service","teach-the-children","the-moving-of-the-water","the-power-of-prayer","the-savior-and-joseph-smith-alike-yet-unlike","the-sixth-day-of-april-1830","the-state-of-the-church","to-draw-closer-to-god","to-honor-the-priesthood","what-god-hath-joined-together","yagottawanna"
                }
            },
            {
                new Conference(ConferencePhase.October, 1991),
                new List<string>()
                {
                    "a-time-for-preparation","and-now-you-will-know","be-an-example-of-the-believers","be-thou-an-example","becoming-self-reliant","bring-up-your-children-in-light-and-truth","called-to-serve","charity-suffereth-long","christ-is-the-light-to-all-mankind","covenants-and-ordinances","daughters-of-god","follow-christ-in-word-and-deed","fruits-of-the-restored-gospel-of-jesus-christ","jesus-the-christ","joy-and-mercy","light","obtaining-help-from-the-lord","our-mission-of-saving","our-solemn-responsibilities","precious-children-a-gift-from-god","rejoice-in-every-good-thing","repentance","reverence-invites-revelation","strengthen-the-feeble-knees","testimony","the-call-an-eternal-miracle","the-conversion-process","the-dual-aspects-of-prayer","the-family-of-the-prophet-joseph-smith","the-gospel-a-global-faith","the-lord-bless-you","the-lords-day","the-ultimate-inheritance-an-allegory","the-voice-is-still-small","these-are-your-days","these-were-our-examples","to-a-missionary-son","today-a-day-of-eternity"
                }
            },
            {
                new Conference(ConferencePhase.April, 1992),
                new List<string>()
                {
                    "a-chosen-generation","a-disciple-of-jesus-christ","a-mighty-force-for-righteousness","a-more-excellent-way","a-prisoner-of-love","an-attitude-of-gratitude","be-men","believe-his-prophets","but-the-labourers-are-few","charity-never-faileth","come-to-the-house-of-the-lord","doors-of-death","faith-and-good-works","gratitude-for-the-goodness-of-god","healing-the-tragic-scars-of-abuse","look-up-and-press-on","memories-of-yesterday-counsel-for-today","my-servant-joseph","nourish-the-flock-of-christ","our-great-mission","our-moral-environment","patience-in-affliction","please-hear-the-call","seeking-the-good","spiritual-healing","spit-and-mud-and-kigatsuku","take-up-his-cross","the-blessings-of-sacrifice","the-mission-of-relief-society","the-pure-love-of-god","the-relief-society-and-the-church","the-royal-law","the-spirit-of-relief-society","the-tongue-can-be-a-sharp-sword","to-learn-to-do-to-be","unclutter-your-life","what-doest-ye-for-christ","you-are-not-alone"
                }
            },
            {
                new Conference(ConferencePhase.October, 1992),
                new List<string>()
                {
                    "a-loving-communicating-god","a-priceless-heritage","a-yearning-for-home","an-example-of-the-believers","at-parting","becoming-wise-unto-salvation","behold-the-lord-hath-shown-unto-me-great-and-marvelous-things","behold-your-little-ones","bible-stories-and-personal-protection","born-of-goodly-parents","building-your-tabernacle","by-the-power-of-his-word-did-they-cause-prisons-to-tumble","by-way-of-invitation-alma-5-62","coming-unto-christ-by-searching-the-scriptures","confidence-through-conversion","fear","healing-your-damaged-life","honour-thy-father-and-thy-mother","jesus-christ-is-at-the-center-of-the-restoration-of-the-gospel","love-of-christ","miracles-then-and-now","missionary-work-in-the-philippines","nobody-said-that-it-would-be-easy","remember-also-the-promises","settle-this-in-your-hearts","sin-will-not-prevail","spiritual-bonfires-of-testimony","spiritual-revival","successful-living-of-gospel-principles","the-beacon-in-the-harbor-of-peace","the-church-is-on-course","the-golden-years","the-joy-of-hope-fulfilled","the-lord-will-prosper-the-righteous","the-priesthood-in-action","to-be-learned-is-good-if","to-the-women-of-the-church","where-is-wisdom"
                }
            },
            {
                new Conference(ConferencePhase.April, 1993),
                new List<string>()
                {
                    "a-prophets-testimony","back-to-gospel-basics","behold-the-enemy-is-combined-d-c-38-12","cats-cradle-of-kindness","come-unto-christ","faith-yields-priesthood-power","father-come-home","gifts","he-maketh-me-to-lie-down-in-green-pastures","heroes","honoring-the-priesthood","i-know-in-whom-i-have-trusted","jesus-christ-the-son-of-the-living-god","jesus-the-very-thought-of-thee","keep-the-faith","keeping-covenants","peace-through-prayer","personal-temple-worship","power-of-the-church-rooted-in-christ","prayer","receiving-divine-assistance-through-the-grace-of-the-lord","search-and-rescue","search-the-scriptures","some-lessons-i-learned-as-a-boy","spiritually-strong-homes-and-families","the-language-of-prayer","the-lord-of-life","the-power-of-correct-principles","the-principle-of-work","the-temple-of-the-lord","the-temple-the-priesthood","this-peaceful-house-of-god","whom-the-lord-calls-the-lord-qualifies"
                }
            },
            {
                new Conference(ConferencePhase.October, 1993),
                new List<string>()
                {
                    "a-mighty-change-of-heart","acquiring-spiritual-knowledge","an-eternal-vision","be-of-good-cheer","bring-up-a-child-in-the-way-he-should-go","choose-the-right","combatting-spiritual-drift-our-global-pandemic","constancy-amid-change","divine-forgiveness","equality-through-diversity","for-time-and-all-eternity","from-the-beginning","gratitude","how-will-our-children-remember-us","keeping-covenants-and-honoring-the-priesthood","look-to-god-and-live","meeting-lifes-challenges","missionary-work-our-responsibility","my-testimony","our-lord-and-savior","ponder-the-path-of-thy-feet","rearing-children-in-a-polluted-environment","relief-society-charity-the-guiding-principle","service-and-happiness","strength-in-counsel","strength-in-the-savior","take-time-for-your-children","the-great-plan-of-happiness","the-lords-wind","the-modern-mighty-of-israel","the-search-for-happiness","the-upward-reach","touch-not-the-evil-gift-nor-the-unclean-thing","truth-is-the-issue","ward-and-branch-families-part-of-heavenly-fathers-plan-for-us","your-personal-checklist-for-a-successful-eternal-flight"
                }
            },
            {
                new Conference(ConferencePhase.April, 1994),
                new List<string>()
                {
                    "a-childs-love-matured","a-divine-prescription-for-spiritual-healing","counseling-with-our-councils","courage-to-hearken","decisions","faith-in-the-lord-jesus-christ","faith-is-the-answer","feed-my-sheep","five-loaves-and-two-fishes","god-is-at-the-helm","gratitude","growing-up-spiritually","if-a-man-die-shall-he-live-again","increase-in-faith","jesus-of-nazareth","live-in-obedience","remember-your-covenants","stretching-the-cords-of-the-tent","take-especial-care-of-your-family","teach-us-tolerance-and-love","teaching-children-to-walk-uprightly-before-the-lord","the-father-and-the-family","the-greatest-miracle-in-human-history","the-path-to-peace","the-power-of-a-good-life","the-priesthood-a-sacred-trust","the-special-status-of-children","the-unique-message-of-jesus-christ","therefore-i-was-taught","tithing","to-be-healed","trying-to-be-like-jesus","walk-with-me","we-all-have-a-father-in-whom-we-can-trust","what-he-would-have-us-do","what-manner-of-men-ought-ye-to-be","what-shall-we-do"
                }
            },
            {
                new Conference(ConferencePhase.October, 1994),
                new List<string>()
                {
                    "being-a-righteous-husband-and-father","brightness-of-hope","charity-and-learning","deep-roots","don-t-drop-the-ball","endure-to-the-end-in-charity","exceeding-great-and-precious-promises","follow-the-son-of-god","heed-the-prophets-voice","helping-children-know-truth-from-error","let-us-build-fortresses","make-thee-an-ark","making-the-right-choices","miracles-of-the-restoration","my-brothers-keeper","personal-revelation-the-gift-the-test-and-the-promise","priceless-principles-for-success","restored-truth","rowing-your-boat","save-the-children","seek-and-ye-shall-find","solemn-assemblies","stand-firm-in-the-faith","stand-ye-in-holy-places","teach-the-children","that-thy-confidence-wax-strong","the-fatherless-and-the-widows-beloved-of-god","the-importance-of-receiving-a-personal-testimony","the-keys-that-never-rust","the-kingdom-progresses-in-africa","the-only-true-and-valid-basis","the-revelations-of-heaven","the-simple-things","the-spirit-of-elijah","worship-through-music"
                }
            },
            {
                new Conference(ConferencePhase.April, 1995),
                new List<string>()
                {
                    "a-table-encircled-with-love","a-time-to-choose","always-remember-him","an-elect-lady","answers-to-lifes-questions","apostasy-and-restoration","as-jesus-sees-us","being-leaders-who-foster-growth","celebrating-covenants","children-of-the-covenant","covenant-of-love","deny-yourselves-of-all-ungodliness","easter-reflections","fat-free-feasting","finding-forgiveness","he-will-be-there-to-help","hear-the-prophets-voice-and-obey","heirs-to-the-kingdom-of-god","living-water-to-quench-spiritual-thirst","marriage-and-the-great-plan-of-happiness","mercy-the-divine-gift","our-priesthood-legacy","responsibilities-of-shepherds","search-for-identity","sustaining-a-new-prophet","that-all-may-hear","the-light-within-you","the-power-to-heal-from-within","the-reward-is-worth-the-effort","the-shield-of-faith","the-temple-is-a-family-affair","the-translation-miracle-of-the-book-of-mormon","this-is-the-work-of-the-master","this-work-is-concerned-with-people","trust-in-the-lord","trying-the-word-of-god","watchmen-on-the-tower","we-have-a-work-to-do","we-have-kept-the-faith","ye-shall-feast-upon-this-fruit"
                }
            },
            {
                new Conference(ConferencePhase.October, 1995),
                new List<string>()
                {
                    "a-living-network","a-strategy-for-war","acting-for-ourselves-and-not-being-acted-upon","as-we-gather-together","blessings-of-the-priesthood","encircled-in-the-saviors-love","eternal-laws-of-happiness","hyrum-smith-firm-as-the-pillars-of-heaven","i-will-go","if-ye-are-prepared-ye-shall-not-fear","lord-to-whom-shall-we-go","of-missions-temples-and-stewardship","our-message-to-the-world","patience-a-heavenly-virtue","perfection-pending","powerful-ideas","priesthood-blessings","redeemer-of-israel","relief-society-a-balm-in-gilead","sacrifice-in-the-service","seek-first-the-kingdom-of-god","spiritual-mountaintops","stand-strong-against-the-wiles-of-the-world","stay-the-course-keep-the-faith","swallowed-up-in-the-will-of-the-father","the-book-of-mormon-a-sacred-ancient-record","the-brilliant-morning-of-forgiveness","the-fabric-of-faith-and-testimony","the-family-a-proclamation-to-the-world","the-power-of-goodness","this-do-in-remembrance-of-me","to-touch-a-life-with-faith","touch-the-hearts-of-the-children","trust-in-the-lord","what-is-relief-society-for","who-honors-god-god-honors","windows-of-light-and-truth","witnesses"
                }
            },
            {
                new Conference(ConferencePhase.April, 1996),
                new List<string>()
                {
                    "a-handful-of-meal-and-a-little-oil","a-legacy-of-testimony","an-anchor-for-eternity-and-today","baskets-and-bottles","be-ye-clean","becometh-as-a-child","commitment","conversion-and-commitment","duty-calls","facing-trials-with-optimism","faith-of-our-fathers","feasting-at-the-lords-table","finding-joy-in-life","he-has-given-me-a-prophet","if-thou-wilt-enter-into-life-keep-the-commandments","joseph-the-man-and-the-prophet","listening-with-new-ears","my-prayers-were-answered","remember-how-thou-hast-received-and-heard","remember-thy-church-o-lord","sacrament-of-the-lords-supper","spiritual-shepherds","stand-true-and-faithful","stay-on-the-true-course","sustaining-the-living-prophets","temptation","the-prophetic-voice","the-sabbath-day-and-sunday-shopping","the-way-of-the-master","the-word-of-wisdom-the-principle-and-the-promises","this-glorious-easter-morn","this-work-is-true","thou-shalt-have-no-other-gods","what-i-want-my-son-to-know-before-he-leaves-on-his-mission","ye-may-know"
                }
            },
            {
                new Conference(ConferencePhase.October, 1996),
                new List<string>()
                {
                    "according-to-the-desire-of-our-hearts","always-have-his-spirit","be-thou-an-example","behold-your-little-ones","christ-at-bethesdas-pool","christians-in-belief-and-action","confirmed-in-faith","covenant-marriage","faith-in-every-footstep","honesty-a-moral-compass","listen-by-the-power-of-the-spirit","listening-to-the-voice-of-the-lord","partakers-of-the-glories","prophets-are-inspired","raised-in-hope","reach-with-a-rescuing-hand","rejoice","run-and-not-be-weary","strengthened-in-charity","the-atonement","the-eternal-family","the-grand-key-words-for-the-relief-society","the-joy-of-living-the-great-plan-of-happiness","the-ordinary-classroom-a-powerful-place-for-steady-and-continued-growth","the-peaceable-things-of-the-kingdom","the-savior-is-counting-on-you","the-spirit-of-prophecy","the-twelve-apostles","this-thing-was-not-done-in-a-corner","we-care-enough-to-send-our-very-best","witnesses-for-god","woman-why-weepest-thou","women-of-the-church"
                }
            },
            {
                new Conference(ConferencePhase.April, 1997),
                new List<string>()
                {
                    "a-holy-calling","a-righteous-choice","a-small-stone","as-good-as-our-bond","because-she-is-a-mother","bishop-help","caring-for-the-souls-of-children","converts-and-young-men","draw-nearer-to-christ","endure-and-be-lifted-up","eternity-lies-before-us","finding-faith-in-every-footstep","finding-safety-in-counsel","friends-standing-together","from-whom-all-blessings-flow","go-and-do-thou-likewise","gratitude","his-peace","in-his-strength-i-can-do-all-things","jesus-christ-our-redeemer","keep-walking-and-give-time-a-chance","may-we-be-faithful-and-true","modern-pioneers","our-testimony-to-the-world","pioneers-all","power-of-the-priesthood","pray-unto-the-father-in-my-name","that-spirit-which-leadeth-to-do-good","the-basics-have-not-changed","they-showed-the-way","they-will-come","true-to-the-faith","true-to-the-truth","washed-clean","when-thou-art-converted-strengthen-thy-brethren","you-have-nothing-to-fear-from-the-journey"
                }
            },
            {
                new Conference(ConferencePhase.October, 1997),
                new List<string>()
                {
                    "a-celestial-connection-to-your-teenage-years","apply-the-atoning-blood-of-christ","are-you-the-woman-i-think-you-are","behold-the-man","called-to-serve","care-for-new-converts","creating-places-of-security","daughter-be-of-good-comfort","drawing-nearer-to-the-lord","feed-my-lambs","following-the-pioneers","for-such-a-time-as-this","four-absolute-truths-provide-an-unfailing-moral-compass","he-hath-filled-the-hungry-with-good-things","home-teaching-a-divine-service","hymn-of-the-obedient-all-is-well","in-remembrance-of-jesus","latter-day-saints-in-very-deed","look-to-the-future","making-faith-a-reality","pioneer-shoes-through-the-ages","pioneers-of-the-future-be-not-afraid-only-believe","receive-truth","some-thoughts-on-temples-retention-of-converts-and-missionary-service","spiritual-capacity","standing-for-truth-and-right","teach-the-children","teachers-the-timeless-key","the-home-a-refuge-and-sanctuary","the-lord-blesses-his-children-through-patriarchal-blessings","the-mighty-strength-of-the-relief-society","the-plan-of-salvation-a-flight-plan-for-life","the-weightier-matters-of-the-law-judgment-mercy-and-faith","universal-application-of-the-gospel","valued-companions","why-every-member-a-missionary"
                }
            },
            {
                new Conference(ConferencePhase.April, 1998),
                new List<string>()
                {
                    "a-disciple-a-friend","a-new-harvest-time","a-teacher-come-from-god","agency-and-anger","behold-we-count-them-happy-which-endure","bridging-the-gap-between-uncertainty-and-certainty","children-and-the-family","christ-can-change-human-behavior","come-unto-christ","have-you-been-saved","how-near-to-the-angels","in-harms-way","live-the-commandments","living-worthy-of-the-girl-you-will-someday-marry","look-to-god-and-live","marvelous-are-the-revelations-of-the-lord","missionary-service","new-temples-to-provide-crowning-blessings-of-the-gospel","obedience-lifes-great-challenge","put-your-shoulder-to-the-wheel","removing-barriers-to-happiness","search-me-o-god-and-know-my-heart","teaching-our-children-to-love-the-scriptures","testimony","that-we-may-be-one","the-articles-of-faith","the-heart-and-a-willing-mind","the-kingdoms-perfecting-pathway","the-relief-society","the-time-to-prepare","tithing-a-privilege","turning-hearts-to-the-family","understanding-our-true-identity","we-bear-witness-of-him","we-seek-after-these-things","young-women-titles-of-liberty"
                }
            },
            {
                new Conference(ConferencePhase.October, 1998),
                new List<string>()
                {
                    "a-season-of-opportunity","a-voice-of-warning","are-we-keeping-pace","as-for-me-and-my-house-we-will-serve-the-lord","bear-record-of-him","benediction","by-what-power-have-ye-done-this","come-let-us-walk-in-the-light-of-the-lord","come-listen-to-a-prophets-voice","come-to-relief-society","cultivating-divine-attributes","establishing-the-church","gratitude","healing-soul-and-body","hope-through-the-atonement-of-jesus-christ","obeying-the-law-serving-ones-neighbor","opening-the-windows-of-heaven","overcoming-discouragement","parents-in-zion","pearls-from-the-sand","personal-purity","small-temples-large-blessings","sustaining-the-prophets","the-aaronic-priesthood-and-the-sacrament","the-living-prophet-our-source-of-pure-doctrine","the-power-of-righteousness","the-priesthood-quorum","think-to-thank","to-the-boys-and-to-the-men","today-determines-tomorrow","walking-in-the-light-of-the-lord","we-are-children-of-god","we-are-not-alone","welcome-to-conference","what-are-people-asking-about-us","ye-also-shall-bear-witness","youth-of-the-noble-birthright"
                }
            },
            {
                new Conference(ConferencePhase.April, 1999),
                new List<string>()
                {
                    "bridges-and-eternal-keepsakes","fellowshipping","find-the-lambs-feed-the-sheep","follow-the-light","for-i-was-blind-but-now-i-see","friendship-a-gospel-principle","greed-selfishness-and-overindulgence","he-is-not-here-but-is-risen","inspired-church-welfare","like-a-flame-unquenchable","love-and-service","made-like-unto-the-son-of-god","obedience-the-path-to-freedom","our-only-chance","our-sacred-duty-to-honor-women","out-of-small-things","preparing-our-families-for-the-temple","priesthood-and-the-home","receive-the-temple-blessings","repent-of-our-selfishness-d-c-56-8","spiritual-power-of-our-baptism","strengthening-families-our-sacred-duty","teach-them-the-word-of-god-with-all-diligence","thanks-to-the-lord-for-his-blessings","the-bishop-and-his-counselors","the-hands-of-the-fathers","the-power-of-teaching-doctrine","the-priesthood-mighty-army-of-the-lord","the-shepherds-of-the-flock","the-witness-martin-harris","the-work-moves-forward","this-is-our-day","true-followers","welcome-home","your-celestial-journey","your-light-in-the-wilderness","your-name-is-safe-in-our-home"
                }
            },
            {
                new Conference(ConferencePhase.October, 1999),
                new List<string>()
                {
                    "a-testimony-of-the-book-of-mormon","a-year-of-jubilee","agency-a-blessing-and-a-burden","an-high-priest-of-good-things-to-come","at-the-summit-of-the-ages","becoming-our-best-selves","behold-the-man","beware-of-false-prophets-and-false-teachers","do-not-delay","feed-my-sheep","for-this-cause-came-i-into-the-world","good-bye-to-this-wonderful-old-tabernacle","gospel-teaching","growing-into-the-priesthood","he-lives","home-family-and-personal-enrichment","hope-an-anchor-of-the-soul","lessons-from-laman-and-lemuel","no-man-is-an-island","of-seeds-and-soils","one-link-still-holds","our-destiny","our-legacy","peace-hope-and-direction","priesthood-power","prophets-and-spiritual-mole-crickets","rejoice-daughters-of-zion","righteousness","serving-the-lord","spiritual-hurricanes","the-faith-of-a-sparrow-faith-and-trust-in-the-lord-jesus-christ","the-spirit-of-revelation","the-tongue-of-angels","we-are-women-of-god","welcome-to-conference","what-it-means-to-be-a-daughter-of-god","why-we-do-some-of-the-things-we-do"
                }
            },
            {
                new Conference(ConferencePhase.April, 2000),
                new List<string>()
                {
                    "a-brief-introduction-to-the-church","a-temple-for-west-africa","a-time-of-new-beginnings","are-you-still-here","as-doves-to-our-windows","because-my-father-sent-me","content-with-the-things-allotted-unto-us","faith-devotion-and-gratitude","finding-a-safe-harbor","future-leaders","heavenly-father-has-a-special-plan","honoring-the-priesthood","how-is-it-with-us","integrity","keep-an-eternal-perspective","living-happily-ever-after","my-testimony","resurrection","stand-as-a-witness","standing-with-god","the-cloven-tongues-of-fire","the-creation","the-power-of-self-mastery","the-sanctity-of-womanhood","the-shield-of-faith","the-stake-president","the-widows-of-zion","thou-shalt-give-heed-unto-all-his-words","to-all-the-world-in-testimony","watch-over-and-strengthen","we-are-creators","womanhood-the-highest-place-of-honor","your-eternal-home","your-eternal-voyage","your-own-personal-testimony"
                }
            },
            {
                new Conference(ConferencePhase.October, 2000),
                new List<string>()
                {
                    "a-great-family-in-reverence-and-worship","a-growing-testimony","an-humble-and-a-contrite-heart","be-a-strong-link","come-and-see","cultivate-righteous-traditions","dedication-day","discipleship","freedom-from-or-freedom-to","great-shall-be-the-peace-of-thy-children","lead-kindly-light","living-by-scriptural-guidance","living-prophets-seers-and-revelators","now-is-the-time","one-by-one","pure-testimony","retaining-a-remission-of-sin","ripples","sanctify-yourselves","satans-bag-of-snipes","seeking-the-spirit-of-god","sharing-the-gospel","stand-tall-and-stand-together","testimony","the-blessing-of-keeping-the-sabbath-day-holy","the-call-to-serve","the-challenge-to-become","the-covenant-of-baptism-to-be-in-the-kingdom-and-of-the-kingdom","the-enemy-within","the-joy-of-womanhood","the-path-to-peace-and-joy","the-redemption-of-the-dead-and-the-testimony-of-jesus","the-tugs-and-pulls-of-the-world","this-great-millennial-year","we-are-instruments-in-the-hands-of-god","write-upon-my-heart","ye-are-the-temple-of-god","your-greatest-challenge-mother"
                }
            },
            {
                new Conference(ConferencePhase.April, 2001),
                new List<string>()
                {
                    "a-comforter-a-guide-a-testifier","a-god-of-miracles","an-invitation-with-promise","born-again","building-a-community-of-saints","building-the-kingdom","compassion","couple-missionaries-a-time-to-serve","david-a-future-missionary","developing-our-talent-for-spirituality","enhancing-our-temple-experience","first-things-first","focus-and-priorities","good-bye-for-another-season","gratitude-and-service","his-word-ye-shall-receive","how-can-i-become-the-woman-of-whom-i-dream","personal-preparation-for-temple-blessings","plow-in-hope","priesthood-power","sacrifice-an-eternal-investment","the-law-of-the-fast","the-miracle-of-faith","the-perpetual-education-fund","the-touch-of-the-masters-hand","the-work-goes-on","them-that-honour-me-i-will-honour","to-bear-testimony-of-mine-only-begotten","to-the-rescue","to-walk-humbly-with-thy-god","united-in-love-and-testimony","watch-with-me","witnesses-unto-me","you-can-t-pet-a-rattlesnake","your-celestial-guide"
                }
            },
            {
                new Conference(ConferencePhase.October, 2001),
                new List<string>()
                {
                    "are-we-not-all-mothers","be-thou-an-example","beware-of-murmuring","building-a-bridge-of-faith","create-or-continue-priesthood-links","doctrine-of-inclusion","duty-calls","faith-of-our-prophets","fear-not-for-they-that-be-with-us-are-more","fulfilling-our-duty-to-god","gratitude","help-thou-mine-unbelief","it-is-not-good-for-man-or-woman-to-be-alone","like-a-watered-garden","living-in-the-fulness-of-times","now-is-the-time","one-step-after-another","our-actions-determine-our-character","our-duty-to-god","our-fathers-plan","prayer","reaching-down-to-lift-another","set-in-order-thy-house","sharing-the-gospel","some-great-thing","stand-firm","standing-tall","steadfast-and-immovable","the-atonement-our-greatest-hope","the-book-of-mormon-another-testament-of-jesus-christ","the-first-and-great-commandment","the-power-of-a-strong-testimony","the-returned-missionary","the-seventh-commandment-a-shield","the-times-in-which-we-live","till-we-meet-again","writing-gospel-principles-in-our-hearts"
                }
            },
            {
                new Conference(ConferencePhase.April, 2002),
                new List<string>()
                {
                    "becoming-a-great-benefit-to-our-fellow-beings","becoming-men-in-whom-the-spirit-of-god-is","being-teachable","charity-perfect-and-everlasting-love","children","consecrate-thy-performance","developing-inner-strength","eternal-life-through-jesus-christ","faith-obedience","feel-the-love-of-the-lord","follow-me","for-thy-good","full-conversion-brings-happiness","hidden-wedges","hold-high-the-torch","how-firm-our-foundation","i-ll-go-where-you-want-me-to-go","it-can-t-happen-to-me","out-of-darkness-into-his-marvelous-light","pathways-to-perfection","personal-worthiness-to-exercise-the-priesthood","some-basic-teachings-from-the-history-of-joseph-smith","standing-in-holy-places","strengthen-home-and-family","the-church-goes-forward","the-gospel-in-our-lives","the-language-of-love","the-law-of-tithing","the-lifeline-of-prayer","the-opportunity-to-serve","the-other-prodigal","the-peaceable-things-of-the-kingdom","they-pray-and-they-go","this-road-we-call-life","true-friends","we-look-to-christ","we-walk-by-faith"
                }
            },
            {
                new Conference(ConferencePhase.October, 2002),
                new List<string>()
                {
                    "a-voice-of-gladness-for-our-children","a-woman-of-faith","blessed-are-the-peacemakers","blessing-our-families-through-our-covenants","but-if-not","called-of-god","called-to-serve","charity-one-family-one-home-at-a-time","come-to-zion-come-to-zion","dad-are-you-awake","each-a-better-person","encircled-in-the-arms-of-his-love","fun-and-happiness","i-believe-i-can-i-knew-i-could","i-ll-go-where-you-want-me-to-go","models-to-follow","o-that-i-were-an-angel-and-could-have-the-wish-of-mine-heart","peace-be-still","rise-to-your-call","sacrifice-brings-forth-the-blessings-of-heaven","shall-he-find-faith-on-the-earth","that-they-may-be-one-in-us","the-global-church-blessed-by-the-voice-of-the-prophets","the-greatest-generation-of-missionaries","the-marvelous-foundation-of-our-faith","the-stake-patriarch","tithing-a-test-of-faith-with-eternal-blessings","to-be-free-of-heavy-burdens","to-men-of-the-priesthood","were-there-not-ten-cleansed","whats-in-it-for-me","with-holiness-of-heart","yielding-to-the-enticings-of-the-holy-spirit","you-are-all-heaven-sent"
                }
            },
            {
                new Conference(ConferencePhase.April, 2003),
                new List<string>()
                {
                    "a-child-and-a-disciple","a-prayer-for-the-children","and-thats-the-way-it-is","benediction","blessed-by-living-water","care-for-the-life-of-the-soul","dear-are-the-sheep-that-have-wandered","did-i-tell-you","eternal-marriage","faith-through-tribulation-brings-peace-and-joy","follow-the-instructions","forgiveness-will-change-bitterness-to-love","give-thanks-in-all-things","growing-into-the-priesthood","holy-place-sacred-space","i-can-pray-to-heavenly-father-anytime-anywhere","in-search-of-treasure","loyalty","overcoming-the-stench-of-sin","preparing-for-missionary-service","press-forward-and-be-steadfast","seek-and-ye-shall-find","show-you-know","stand-in-your-appointed-place","steadfast-in-our-covenants","sweet-power-of-prayer","the-condition-of-the-church","the-devils-throat","the-essential-role-of-member-missionary-work","the-golden-years","the-importance-of-the-family","the-light-of-his-love","the-sustaining-power-of-faith-in-times-of-uncertainty-and-testing","the-unspeakable-gift","the-virtues-of-righteous-daughters-of-god","there-is-hope-smiling-brightly-before-us","war-and-peace","words-to-live-by","you-are-a-child-of-god"
                }
            },
            {
                new Conference(ConferencePhase.October, 2003),
                new List<string>()
                {
                    "a-sure-foundation","an-enduring-testimony-of-the-mission-of-the-prophet-joseph","an-ensign-to-the-nations-a-light-to-the-world","are-you-a-saint","bring-him-home","choose-ye-therefore-christ-the-lord","choosing-charity-that-good-part","come-follow-me","he-knows-us-he-loves-us","how-choice-a-seer","in-covenant-with-him","let-our-voices-be-heard","let-us-live-the-gospel-more-fully","lord-i-believe-help-thou-mine-unbelief","personal-priesthood-responsibility","priesthood-keys-and-the-power-to-bless","realize-your-full-potential","receiving-a-testimony-of-the-restored-gospel-of-jesus-christ","repentance-and-change","seeing-the-promises-afar-off","the-atonement-repentance-and-dirty-linen","the-bridge-builder","the-clarion-call-of-prophets","the-empowerment-of-humility","the-grandeur-of-god","the-lord-thy-god-will-hold-thy-hand","the-message-of-the-restoration","the-phenomenon-that-is-you","the-shepherds-of-israel","the-standard-of-truth-has-been-erected","the-state-of-the-church","three-choices","to-the-women-of-the-church","we-believe-all-that-god-has-revealed","young-men-holders-of-keys"
                }
            },
            {
                new Conference(ConferencePhase.April, 2004),
                new List<string>()
                {
                    "a-mother-heart","abide-in-me","all-things-shall-work-together-for-your-good","applying-the-simple-and-plain-gospel-principles-in-the-family","believe","but-if-not","choices","concluding-remarks","did-you-get-the-right-message","do-not-fear","earthly-debts-heavenly-debts","fatherhood-an-eternal-calling","for-the-strength-of-youth","how-great-the-wisdom-and-the-love","how-to-live-well-amid-increasing-evil","i-was-an-hungred-and-ye-gave-me-meat","in-the-strength-of-the-lord","jesus-the-very-thought-of-thee","marriage-and-family-our-sacred-responsibility","my-soul-delighteth-in-the-scriptures","preparation-for-the-second-coming","remember-how-merciful-the-lord-hath-been","roots-and-branches","standing-spotless-before-the-lord","stay-on-the-high-road","strengthen-thy-brethren","the-atonement-all-for-all","the-atonement-and-the-value-of-one-soul","the-call-for-courage","the-church-grows-stronger","the-dawning-of-a-brighter-day","the-finished-story","the-words-of-christ-our-spiritual-liahona","when-thou-art-converted","with-all-the-feeling-of-a-tender-parent-a-message-of-hope-to-families","your-personal-influence"
                }
            },
            {
                new Conference(ConferencePhase.October, 2004),
                new List<string>()
                {
                    "a-tragic-evil-among-us","anxiously-engaged","be-not-deceived","belonging-is-our-sacred-birthright","bringing-peace-and-healing-to-your-soul","choose-you-this-day","closing-remarks","condition-of-the-church","faith-and-keys","feed-my-sheep","finding-faith-in-the-lord-jesus-christ","how-has-relief-society-blessed-your-life","i-stand-at-the-door-and-knock","if-ye-are-prepared-ye-shall-not-fear","in-the-strength-of-the-lord","keeping-our-covenants","more-holiness-give-me","out-of-small-things","peace-of-conscience-and-peace-of-mind","perilous-times","press-on","prophets-seers-and-revelators","pure-testimony","remember-the-teachings-of-your-father","securing-our-testimonies","senior-missionaries-and-the-gospel","the-blessings-of-a-proper-fast","the-key-of-the-knowledge-of-god","the-least-of-these","the-opportunity-to-testify","the-power-of-gods-love","the-women-in-our-lives","walking-towards-the-light-of-his-love","we-did-this-for-you","what-is-a-quorum","where-do-i-make-my-stand"
                }
            },
            {
                new Conference(ConferencePhase.April, 2005),
                new List<string>()
                {
                    "a-still-small-voice-and-a-throbbing-heart","a-work-for-me-to-do","all-thy-children-shall-be-taught","appreciating-the-counsel-of-those-who-are-bowed-in-years","be-of-good-cheer-and-faithful-in-adversity","be-thou-an-example","beware-of-the-evil-behind-the-smiling-eyes","closing-remarks","constant-truths-for-changing-times","couple-missionaries-blessings-from-sacrifice-and-service","faith-is-the-answer","gambling","glad-tidings-from-cumorah","he-knows-you-by-name","hearts-bound-together","now-is-the-time-to-prepare","one-more","opening-remarks","our-most-distinguishing-feature","perseverance","pornography","standing-in-holy-places","strengthen-thy-brethren","the-book-of-mormon-another-testament-of-jesus-christ-plain-and-precious-things","the-fruits-of-the-first-vision","the-great-things-which-god-has-revealed","the-power-of-preach-my-gospel","the-sacred-call-of-service","the-tender-mercies-of-the-lord","the-virtue-of-kindness","the-worth-of-souls","tithing-a-commandment-even-for-the-destitute","what-greater-goodness-can-we-know-christlike-friends","what-seek-ye","whos-on-the-lords-side-who"
                }
            },
            {
                new Conference(ConferencePhase.October, 2005),
                new List<string>()
                {
                    "a-pattern-for-all","be-prepared-be-ye-strong-from-henceforth","becoming-a-missionary","benediction","blessings-resulting-from-reading-the-book-of-mormon","called-and-chosen","christlike-attributes-the-wind-beneath-our-wings","compass-of-the-lord","do-your-duty-that-is-best","feed-my-sheep","forgiveness","gospel-covenants-bring-promised-blessings","if-christ-had-my-opportunities","if-ye-are-prepared-ye-shall-not-fear","instruments-in-the-hands-of-god","jesus-christ-the-master-healer","journey-to-higher-ground","knowing-the-lords-will-for-you","mans-search-for-divine-truth","my-soul-delighteth-in-the-scriptures","on-zions-hill","opening-remarks","preparations-for-the-restoration-and-the-second-coming-my-hand-shall-be-over-thee","priesthood-authority-in-the-family-and-the-church","sacrifice-is-a-joy-and-a-blessing","spiritual-preparedness-start-early-and-be-steady","sweet-moments","that-we-may-all-sit-down-in-heaven-together","the-blessings-of-general-conference","the-book-of-mormon-the-instrument-to-gather-scattered-israel","the-light-in-their-eyes","the-prophet-joseph-smith-teacher-by-example","the-sanctity-of-the-body","to-young-women","true-happiness-a-conscious-decision","truth-restored","what-matters-most-is-what-lasts-longest"
                }
            },
            {
                new Conference(ConferencePhase.April, 2006),
                new List<string>()
                {
                    "a-royal-priesthood","all-men-everywhere","an-outpouring-of-blessings","as-a-child","as-now-we-take-the-sacrament","broken-things-to-mend","creating-a-gospel-sharing-home","i-am-the-light-which-ye-shall-hold-up","i-will-remember-your-sins-no-more","instruments-of-the-lords-peace","it-shows-in-your-face","now-is-the-time-to-serve-a-mission","nurturing-marriage","our-rising-generation","our-sacred-priesthood-trust","prayer-faith-and-family-stepping-stones-to-eternal-happiness","repentance-a-blessing-of-membership","see-the-end-from-the-beginning","seek-ye-the-kingdom-of-god","tender-hearts-and-helping-hands","that-we-may-always-have-his-spirit-to-be-with-us","the-abundant-life","the-gift-of-agency","the-great-plan-of-happiness","the-need-for-greater-kindness","the-restoration-of-all-things","to-act-for-ourselves-the-gift-and-blessings-of-agency","to-grow-up-unto-the-lord","true-to-the-faith","until-again-we-meet","you-have-a-noble-birthright","your-light-a-standard-to-all-nations","your-mission-will-change-everything","zion-in-the-midst-of-babylon"
                }
            },
            {
                new Conference(ConferencePhase.October, 2006),
                new List<string>()
                {
                    "a-defense-and-a-refuge","a-priesthood-quorum","and-nothing-shall-offend-them","becoming-instruments-in-the-hands-of-god","behold-your-little-ones","closing-remarks","discipleship","eternally-encircled-in-his-love","faith-service-constancy","he-heals-the-heavy-laden","he-trusts-us","holy-scriptures-the-power-of-god-unto-our-salvation","how-firm-a-foundation","in-the-arms-of-his-love","let-us-be-men","look-toward-eternity","moving-closer-to-him","o-be-wise","prophets-in-the-land-again","receiving-by-the-spirit","remembering-the-lords-love","rise-up-o-men-of-god","spiritual-nutrients","sunday-will-come","that-they-might-know-thee","the-atonement-can-clean-reclaim-and-sanctify-our-lives","the-atonement-can-secure-your-peace-and-happiness","the-faith-to-move-mountains","the-first-generation","the-gathering-of-scattered-israel","the-great-and-wonderful-love","the-great-plan-of-happiness","the-law-of-tithing","the-plan-of-salvation","the-power-of-a-personal-testimony","the-power-of-patience","the-temple-is-about-families","three-towels-and-a-25-cent-newspaper","to-look-reach-and-come-unto-christ","true-to-our-priesthood-trust","we-bear-testimony-to-the-world","wherefore-settle-this-in-your-hearts"
                }
            },
            {
                new Conference(ConferencePhase.April, 2007),
                new List<string>()
                {
                    "a-lesson-from-the-book-of-mormon","a-tabernacle-in-the-wilderness","closing-remarks","commitment-to-the-lord","daughters-of-heavenly-father","divorce","do-you-know","gratitude-a-path-to-happiness","i-am-clean","i-know-that-my-redeemer-lives","if-these-old-walls-could-talk","its-true-isn-t-it-then-what-else-matters","lay-up-in-store","let-virtue-garnish-thy-thoughts-unceasingly","lifes-lessons-learned","message-to-my-grandsons","mom-are-we-christians","point-of-safe-return","prophets-pioneer-and-modern-day","remember-and-perish-not","remembering-repenting-and-changing","repentance-and-conversion","salt-lake-tabernacle-rededication","stay-on-the-path","tabernacle-memories","the-healing-power-of-forgiveness","the-message-of-the-restoration","the-miracle-of-the-holy-bible","the-nourishing-power-of-hymns","the-priesthood-a-sacred-gift","the-spirit-of-the-tabernacle","the-things-of-which-i-know","the-tongue-of-angels","this-day","to-the-aaronic-priesthood-preparing-for-the-decade-of-decision","using-the-supernal-gift-of-prayer","whos-on-the-lords-side","will-a-man-rob-god","ye-must-be-born-again"
                }
            },
            {
                new Conference(ConferencePhase.October, 2007),
                new List<string>()
                {
                    "a-broken-heart-and-a-contrite-spirit","a-royal-priesthood","after-all-we-can-do","blessed-are-all-the-pure-in-heart","claim-the-exceeding-great-and-precious-promises","clean-hands-and-a-pure-heart","closing-remarks","do-it-now","don-t-leave-for-tomorrow-what-you-can-do-today","enduring-together","faith-family-facts-and-fruits","feed-my-sheep","god-helps-the-faithful-priesthood-holder","good-better-best","have-we-not-reason-to-rejoice","i-will-strengthen-thee-i-will-help-thee","knowing-that-we-know","live-by-faith-and-not-by-fear","mothers-who-know","mrs-patton-the-story-continues","nourished-by-the-good-word-of-god","o-remember-remember","out-of-small-things","personal-revelation-the-teachings-and-examples-of-the-prophets","preach-my-gospel-the-unifying-tool-between-members-and-missionaries","quench-not-the-spirit-which-quickens-the-inner-man","raising-the-bar","scriptural-witnesses","service","slow-to-anger","small-and-simple-things","strengthen-home-and-family","the-great-commandment","the-only-true-god-and-jesus-christ-whom-he-hath-sent","the-power-of-godliness-is-manifested-in-the-temples-of-god","the-stone-cut-out-of-the-mountain","the-weak-and-the-simple-of-the-church","three-goals-to-guide-you","today-is-the-time","truth-the-foundation-of-correct-decisions","what-latter-day-saint-women-do-best-stand-strong-and-immovable","why-are-we-members-of-the-only-true-church"
                }
            },
            {
                new Conference(ConferencePhase.April, 2008),
                new List<string>()
                {
                    "a-12-year-old-deacon","a-book-with-a-promise","a-matter-of-a-few-degrees","abundantly-blessed","anchors-of-testimony","and-who-is-my-neighbor","ask-in-faith","at-all-times-in-all-things-and-in-all-places","born-again","concern-for-the-one","daughters-of-god","do-you-know-who-you-are","examples-of-righteousness","faith-and-the-oath-and-covenant-of-the-priesthood","faith-of-our-father","gaining-a-testimony-of-god-the-father-his-son-jesus-christ-and-the-holy-ghost","give-heed-unto-the-prophets-words","looking-back-and-moving-forward","my-soul-delighteth-in-the-things-of-the-lord","my-words-never-cease","one-among-the-crowd","opening-our-hearts","restoring-faith-in-the-family","righteous-traditions","salvation-and-exaltation","service-a-divine-quality","special-experiences","stand-as-a-witness","testimony","the-best-investment","the-gospel-of-jesus-christ","the-power-of-light-and-truth","the-true-and-living-church","the-twelve","three-presiding-high-priests","to-heal-the-shattering-consequences-of-abuse","today","walk-in-the-light","we-will-not-yield-we-cannot-yield"
                }
            },
            {
                new Conference(ConferencePhase.October, 2008),
                new List<string>()
                {
                    "a-return-to-virtue","arms-of-safety","because-my-father-read-the-book-of-mormon","celestial-marriage","christian-courage-the-price-of-discipleship","come-to-zion","come-what-may-and-love-it","even-a-child-can-understand","finding-joy-in-the-journey","fulfilling-the-purpose-of-relief-society","go-ye-therefore","god-loves-and-helps-all-of-his-children","gospel-teaching-our-most-important-calling","happiness-your-heritage","holy-temples-sacred-covenants","honor-the-priesthood-and-use-it-well","hope-ya-know-we-had-a-hard-time","let-him-do-it-with-simplicity","lift-where-you-stand","now-let-us-rejoice","o-ye-that-embark","our-hearts-knit-as-one","pray-always","returning-home","sacrament-meeting-and-the-sacrament","testimony-as-a-process","the-infinite-power-of-hope","the-ministry-of-angels","the-test","the-truth-of-god-shall-go-forth","the-way","to-learn-to-do-to-be","until-we-meet-again","welcome-to-conference","winning-the-war-against-evil","you-know-enough"
                }
            },
            {
                new Conference(ConferencePhase.April, 2009),
                new List<string>()
                {
                    "a-virtuous-life-step-by-step","adversity","be-of-good-cheer","be-thou-an-example-of-the-believers","be-your-best-self","becoming-provident-providers-temporally-and-spiritually","bring-souls-unto-me","come-let-us-go-up-to-the-mountain-of-the-lord","come-unto-him","counsel-to-young-men","faith-in-adversity","faith-in-the-lord-jesus-christ","finding-strength-in-challenging-times","get-on-with-our-lives","gifts-to-help-us-navigate-our-life","his-arm-is-sufficient","his-servants-the-prophets","honorably-hold-a-name-and-standing","learning-the-lessons-of-the-past","lessons-from-the-lords-prayers","man-down","may-you-have-courage","none-were-with-him","our-fathers-plan-big-enough-for-all-his-children","priesthood-responsibilities","respect-and-reverence","revealed-quorum-principles","sacred-homes-sacred-temples","temple-worship-the-source-of-strength-and-power-in-times-of-need","the-power-of-covenants","the-way-of-the-disciple","this-is-your-phone-call","unselfish-service","until-we-meet-again","we-are-doing-a-great-work-and-cannot-come-down","welcome-to-conference"
                }
            },
            {
                new Conference(ConferencePhase.October, 2009),
                new List<string>()
                {
                    "a-call-to-the-rising-generation","an-easiness-and-willingness-to-believe","ask-seek-knock","attempting-the-impossible","be-ready","becoming-more-powerful-priesthood-holders","being-temperate-in-all-things","blessings-of-the-gospel-available-to-all","closing-remarks","every-woman-needs-relief-society","fathers-and-sons-a-remarkable-relationship","helping-others-recognize-the-whisperings-of-the-spirit","hold-on","i-love-loud-boys","joseph-smith-prophet-of-the-restoration","let-virtue-garnish-your-thoughts","love-and-law","mind-the-gap","moral-discipline","more-diligent-and-concerned-at-home","our-perfect-example","prayer-and-promptings","preserving-the-hearts-mighty-change","relief-society-a-sacred-work","repent-that-i-may-heal-you","safety-for-the-soul","school-thy-feelings-o-my-brother","seeking-to-know-god-our-heavenly-father-and-his-son-jesus-christ","stewardship-a-sacred-trust","teaching-helps-save-lives","that-your-burdens-may-be-light","the-enduring-legacy-of-relief-society","the-love-of-god","the-past-way-of-facing-the-future","to-acquire-spiritual-guidance","two-principles-for-any-economy","welcome-to-conference","what-have-i-done-for-someone-today"
                }
            },
            {
                new Conference(ConferencePhase.April, 2010),
                new List<string>()
                {
                    "a-word-at-closing","act-in-all-diligence","all-things-work-together-for-good","and-upon-the-handmaids-in-those-days-will-i-pour-out-my-spirit","be-of-a-good-courage","continue-in-patience","developing-good-judgment-and-not-judging-others","generations-linked-in-love","he-is-risen","he-lives-all-glory-to-his-name","healing-the-sick","help-them-on-their-way-home","helping-hands-saving-hands","mother-told-me","mothers-and-daughters","mothers-teaching-children-in-the-home","never-never-never-give-up","our-duty-to-god-the-mission-of-parents-and-leaders-to-the-rising-generation","our-path-of-duty","place-no-more-for-the-enemy-of-my-soul","preparation-brings-blessings","remember-who-you-are","tell-me-the-stories-of-jesus","that-our-children-might-see-the-face-of-the-savior","the-blessing-of-scripture","the-divine-call-of-a-missionary","the-magnificent-aaronic-priesthood","the-power-of-the-priesthood","the-rock-of-our-redeemer","things-pertaining-to-righteousness","turn-to-the-lord","watching-with-all-perseverance","we-follow-jesus-christ","welcome-to-conference","when-the-lord-commands","you-are-my-hands","your-happily-ever-after"
                }
            },
            {
                new Conference(ConferencePhase.October, 2010),
                new List<string>()
                {
                    "agency-essential-to-the-plan-of-life","and-of-some-have-compassion-making-a-difference","as-we-meet-together-again","avoiding-the-trap-of-sin","be-an-example-of-the-believers","be-thou-an-example-of-the-believers","because-of-your-faith","charity-never-faileth","cleansing-the-inner-vessel","come-unto-me-with-full-purpose-of-heart-and-i-shall-heal-you","courageous-parenting","daughters-in-my-kingdom-the-history-and-work-of-relief-society","faith-the-choice-is-yours","gospel-learning-and-teaching","he-teaches-us-to-put-off-the-natural-man","let-there-be-light","never-leave-him","o-that-cunning-plan-of-the-evil-one","obedience-to-the-prophets","of-things-that-matter-most","our-very-survival","pride-and-the-priesthood","receive-the-holy-ghost","reflections-on-a-consecrated-life","rest-unto-your-souls","serve-with-the-spirit","stay-on-the-path","steadfast-and-immovable","temple-mirrors-of-eternity-a-testimony-of-family","the-divine-gift-of-gratitude","the-holy-ghost-and-revelation","the-priesthood-of-aaron","the-three-rs-of-choice","the-transforming-power-of-faith-and-character","till-we-meet-again","trust-in-god-then-go-and-do","two-lines-of-communication","what-have-you-done-with-my-name"
                }
            },
            {
                new Conference(ConferencePhase.April, 2011),
                new List<string>()
                {
                    "a-living-testimony","an-ensign-to-the-nations","as-many-as-i-love-i-rebuke-and-chasten","at-parting","become-as-a-little-child","called-to-be-saints","desire","establishing-a-christ-centered-home","face-the-future-with-faith","finding-joy-through-loving-service","followers-of-christ","guardians-of-virtue","guided-by-the-holy-spirit","hope","i-believe-in-being-honest-and-true","its-conference-once-again","lds-women-are-incredible","learning-in-the-priesthood","more-than-conquerors-through-him-that-loved-us","opportunities-to-do-good","preparing-the-world-for-the-second-coming","priesthood-power","remember-this-kindness-begins-with-me","sacred-keys-of-the-aaronic-priesthood","testimony","the-atonement-covers-all-pain","the-essence-of-discipleship","the-eternal-blessings-of-marriage","the-holy-temple-a-beacon-to-the-world","the-lords-richest-blessings","the-miracle-of-the-atonement","the-sabbath-and-the-sacrament","the-sanctifying-work-of-welfare","the-spirit-of-revelation","waiting-on-the-road-to-damascus","what-manner-of-men-and-women-ought-ye-to-be","your-potential-your-privilege"
                }
            },
            {
                new Conference(ConferencePhase.October, 2011),
                new List<string>()
                {
                    "a-time-to-prepare","a-witness","as-we-meet-again","charity-never-faileth","children","choose-eternal-life","cleave-unto-the-covenants","counsel-to-youth","covenants","dare-to-stand-alone","doing-the-right-thing-at-the-right-time-without-delay","forget-me-not","it-is-better-to-look-up","love-her-mother","missionaries-are-a-treasure-of-the-church","perfect-love-casteth-out-fear","personal-revelation-and-testimony","preparation-in-the-priesthood-i-need-your-help","providing-in-the-lords-way","redemption","stand-in-holy-places","teaching-after-the-manner-of-the-spirit","teachings-of-jesus","the-book-of-mormon-a-book-from-god","the-divine-gift-of-repentance","the-hearts-of-the-children-shall-turn","the-importance-of-a-name","the-opportunity-of-a-lifetime","the-power-of-scripture","the-power-of-the-aaronic-priesthood","the-privilege-of-prayer","the-songs-they-could-not-sing","the-time-shall-come","until-we-meet-again","waiting-upon-the-lord-thy-will-be-done","we-are-all-enlisted","what-i-hope-my-granddaughters-and-grandsons-will-understand-about-relief-society","you-matter-to-him"
                }
            },
            {
                new Conference(ConferencePhase.April, 2012),
                new List<string>()
                {
                    "aaronic-priesthood-arise-and-use-the-power-of-god","abide-in-the-lords-territory","and-a-little-child-shall-lead-them","arise-and-shine-forth","as-we-close-this-conference","as-we-gather-once-again","believe-obey-and-endure","coming-to-ourselves-the-sacrament-the-temple-and-sacrifice-in-service","converted-to-his-gospel-through-his-church","faith-fortitude-fulfillment-a-message-to-single-parents","families-under-covenant","having-the-vision-to-do","he-truly-loves-us","how-to-obtain-revelation-and-inspiration-for-your-personal-life","in-tune-with-the-music-of-faith","mountains-to-climb","now-is-the-time-to-arise-and-shine","only-upon-the-principles-of-righteousness","sacrifice","seek-learning-you-have-a-work-to-do","special-lessons","teaching-our-children-to-understand","thanks-be-to-god","that-the-lost-may-be-found","the-doctrine-of-christ","the-laborers-in-the-vineyard","the-merciful-obtain-mercy","the-power-of-deliverance","the-powers-of-heaven","the-race-of-life","the-rescue-for-real-growth","the-vision-of-prophets-regarding-relief-society-faith-family-relief","the-why-of-priesthood-service","to-hold-sacred","was-it-worth-it","what-thinks-christ-of-me","willing-and-worthy-to-serve"
                }
            },
            {
                new Conference(ConferencePhase.October, 2012),
                new List<string>()
                {
                    "an-unspeakable-gift-from-god","ask-the-missionaries-they-can-help-you","be-anxiously-engaged","be-valiant-in-courage-strength-and-activity","because-i-live-ye-shall-live-also","becoming-a-true-disciple","becoming-goodly-parents","being-a-more-christian-christian","beware-concerning-yourselves","blessings-of-the-sacrament","brethren-we-have-work-to-do","by-faith-all-things-are-fulfilled","can-ye-feel-so-now","come-unto-me-o-ye-house-of-israel","consider-the-blessings","converted-unto-the-lord","first-observe-then-serve","god-be-with-you-till-we-meet-again","help-them-aim-high","i-know-it-i-live-it-i-love-it","is-faith-in-the-atonement-of-jesus-christ-written-in-our-hearts","learning-with-our-hearts","of-regrets-and-resolutions","one-step-closer-to-the-savior","protect-the-children","see-others-as-they-may-become","temple-standard","the-atonement","the-caregiver","the-first-great-commandment","the-joy-of-redeeming-the-dead","the-joy-of-the-priesthood","the-lord-has-not-forgotten-you","trial-of-your-faith","welcome-to-conference","what-shall-a-man-give-in-exchange-for-his-soul","where-is-the-pavilion","wide-awake-to-our-duties"
                }
            },
            {
                new Conference(ConferencePhase.April, 2013),
                new List<string>()
                {
                    "a-sure-foundation","be-not-moved","beautiful-mornings","being-accepted-of-the-lord","catch-the-wave","come-all-ye-sons-of-god","come-unto-me","followers-of-christ","for-peace-at-home","four-titles","its-a-miracle","lord-i-believe","marriage-watch-and-learn","obedience-brings-blessings","obedience-to-law-is-liberty","personal-peace-the-reward-of-righteousness","redemption","stand-strong-in-holy-places","the-father-and-the-son","the-gospel-to-all-the-world","the-home-the-school-of-life","the-hope-of-gods-light","the-lords-way","the-power-of-the-priesthood-in-the-boy","the-savior-wants-to-forgive","the-words-we-speak","these-things-i-know","this-is-my-work-and-glory","until-we-meet-again","we-are-daughters-of-our-heavenly-father","we-are-one","we-believe-in-being-chaste","welcome-to-conference","when-you-save-a-girl-you-save-generations","your-holy-places","your-sacred-duty-to-minister","your-wonderful-journey-home"
                }
            },
            {
                new Conference(ConferencePhase.October, 2013),
                new List<string>()
                {
                    "be-meek-and-lowly-of-heart","be-ye-converted","bind-up-their-wounds","called-of-him-to-declare-his-word","claim-the-blessings-of-your-covenants","come-join-with-us","continually-holding-fast","decisions-for-eternity","do-we-know-what-we-have","drawing-closer-to-god","general-conference-strengthening-faith-and-testimony","hastening-the-lords-game-plan","i-will-not-fail-thee-nor-forsake-thee","lamentations-of-jeremiah-beware-of-bondage","like-a-broken-vessel","look-ahead-and-believe","look-up","no-other-gods","personal-strength-through-the-atonement-of-jesus-christ","power-in-the-priesthood","put-your-trust-in-the-lord","small-and-simple-things","teaching-with-the-power-and-authority-of-god","the-doctrines-and-principles-contained-in-the-articles-of-faith","the-key-to-spiritual-protection","the-moral-force-of-women","the-power-joy-and-love-of-covenant-keeping","the-strength-to-endure","the-windows-of-heaven","till-we-meet-again","to-my-grandchildren","true-shepherds","we-have-great-reason-to-rejoice","we-never-walk-alone","welcome-to-conference","wilt-thou-be-made-whole","ye-are-no-more-strangers","you-can-do-it-now"
                }
            },
            {
                new Conference(ConferencePhase.April, 2014),
                new List<string>()
                {
                    "a-priceless-heritage-of-hope","are-you-sleeping-through-the-restoration","be-strong-and-of-a-good-courage","bear-up-their-burdens-with-ease","christ-the-redeemer","daughters-in-the-covenant","fear-not-i-am-with-thee","following-up","grateful-in-any-circumstances","i-have-given-you-an-example","if-ye-lack-wisdom","if-ye-love-me-keep-my-commandments","keeping-covenants-protects-us-prepares-us-and-empowers-us","let-your-faith-show","lets-not-take-the-wrong-way","live-true-to-the-faith","love-the-essence-of-the-gospel","obedience-through-our-faithfulness","protection-from-pornography-a-christ-focused-home","roots-and-branches","sisterhood-oh-how-we-need-each-other","spiritual-whirlwinds","the-choice-generation","the-cost-and-blessings-of-discipleship","the-joyful-burden-of-discipleship","the-keys-and-authority-of-the-priesthood","the-priesthood-man","the-prophet-joseph-smith","the-resurrection-of-jesus-christ","the-witness","until-we-meet-again","wanted-hands-and-hearts-to-hasten-the-work","welcome-to-conference","what-are-you-thinking","what-manner-of-men","where-your-treasure-is","your-four-minutes"
                }
            },
            {
                new Conference(ConferencePhase.October, 2014),
                new List<string>()
                {
                    "approaching-the-throne-of-god-with-confidence","are-we-not-all-beggars","choose-wisely","come-and-see","continuing-revelation","covenant-daughters-of-god","eternal-life-to-know-our-heavenly-father-and-his-son-jesus-christ","finding-lasting-peace-and-building-eternal-families","free-forever-to-act-for-themselves","guided-safely-home","i-know-these-things-of-myself","joseph-smith","live-according-to-the-words-of-the-prophets","living-the-gospel-joyful","lord-is-it-i","loving-others-and-living-with-differences","make-the-exercise-of-faith-your-first-priority","our-personal-ministries","parents-the-prime-gospel-teachers-of-their-children","ponder-the-path-of-thy-feet","prepared-in-a-manner-that-never-had-been-known","receiving-a-testimony-of-light-and-truth","rescue-in-unity","sharing-your-light","stay-in-the-boat-and-hold-on","sustaining-the-prophets","the-book","the-law-of-the-fast-a-personal-responsibility-to-care-for-the-poor-and-needy","the-lord-has-a-plan-for-us","the-preparatory-priesthood","the-reason-for-our-hope","the-sacrament-a-renewal-for-the-soul","the-sacrament-and-the-atonement","trifle-not-with-sacred-things","until-we-meet-again","welcome-to-conference","which-way-do-you-face","yes-lord-i-will-follow-thee"
                }
            },
            {
                new Conference(ConferencePhase.April, 2015),
                new List<string>()
                {
                    "be-fruitful-multiply-and-subdue-the-earth","blessings-of-the-temple","choose-to-believe","defenders-of-the-family-proclamation","fatherhood-our-eternal-destiny","filling-our-homes-with-light-and-truth","if-you-will-be-responsible","is-it-still-wonderful-to-you","is-not-this-the-fast-that-i-have-chosen","latter-day-saints-keep-on-trying","on-being-genuine","preserving-agency-protecting-religious-freedom","priesthood-and-personal-prayer","returning-to-faith","seeking-the-lord","stay-by-the-tree","the-comforter","the-eternal-perspective-of-the-gospel","the-family-is-of-god","the-gift-of-grace","the-greatest-generation-of-young-adults","the-lord-is-my-light","the-music-of-the-gospel","the-parable-of-the-sower","the-plan-of-happiness","the-priesthood-a-sacred-gift","the-sabbath-is-a-delight","therefore-they-hushed-their-fears","thy-kingdom-come","truly-good-and-without-guile","waiting-for-the-prodigal","well-ascend-together","where-justice-love-and-mercy-meet","why-marriage-and-family-matter-everywhere-in-the-world","why-marriage-why-family","yes-we-can-and-will-win"
                }
            },
            {
                new Conference(ConferencePhase.October, 2015),
                new List<string>()
                {
                    "a-plea-to-my-sisters","a-summer-with-great-aunt-rose","be-an-example-and-a-light","be-not-afraid-only-believe","behold-thy-mother","blessed-and-happy-are-those-who-keep-the-commandments-of-god","choose-the-light","chosen-to-bear-testimony-of-my-name","discovering-the-divinity-within","eyes-to-see-and-ears-to-hear","faith-is-not-by-chance-but-by-choice","god-is-at-the-helm","here-to-serve-a-righteous-cause","hold-on-thy-way","i-stand-all-amazed","if-ye-love-me-keep-my-commandments","it-works-wonderfully","its-never-too-early-and-its-never-too-late","keep-the-commandments","let-the-clarion-trumpet-sound","meeting-the-challenges-of-todays-world","my-heart-pondereth-them-continually","plain-and-precious-truths","remembering-in-whom-we-have-trusted","shipshape-and-bristol-fashion-be-temple-worthy-in-good-times-and-bad-times","strengthened-by-the-atonement-of-jesus-christ","tested-and-tempted-but-helped","that-they-do-always-remember-him","the-holy-ghost-as-your-companion","the-joy-of-living-a-christ-centered-life","the-pleasing-word-of-god","through-gods-eyes","turn-to-him-and-answers-will-come","what-lack-i-yet","why-the-church","worthy-of-our-promised-blessings","yielding-our-hearts-to-god","you-are-not-alone-in-the-work","your-next-step"
                }
            },
            {
                new Conference(ConferencePhase.April, 2016),
                new List<string>()
                {
                    "a-childs-guiding-gift","a-pattern-for-peace","a-sacred-trust","always-remember-him","always-retain-a-remission-of-your-sins","and-there-shall-be-no-more-death","be-thou-humble","choices","do-i-believe","eternal-families","family-councils","fathers","he-asks-us-to-be-his-hands","he-will-place-you-on-his-shoulders-and-carry-you-home","i-am-a-child-of-god","i-was-a-stranger","in-praise-of-those-who-save","opposition-in-all-things","refuge-from-the-storm","see-yourself-in-the-temple","standing-with-the-leaders-of-the-church","that-i-might-draw-all-men-unto-me","the-greatest-leaders-are-the-greatest-followers","the-healing-ointment-of-forgiveness","the-holy-ghost","the-power-of-godliness","the-price-of-priesthood-power","the-sacred-place-of-restoration","to-the-rescue-we-can-do-it","tomorrow-the-lord-will-do-wonders-among-you","trust-in-that-spirit-which-leadeth-to-do-good","what-shall-we-do","where-are-the-keys-and-authority-of-the-priesthood","where-two-or-three-are-gathered","whoso-receiveth-them-receiveth-me"
                }
            },
            {
                new Conference(ConferencePhase.October, 2016),
                new List<string>()
                {
                    "a-choice-seer-will-i-raise-up","a-witness-of-god","abide-in-my-love","am-i-good-enough-will-i-make-it","be-ambitious-for-christ","come-follow-me-by-practicing-christian-love-and-service","emissaries-to-the-church","for-our-spiritual-development-and-learning","fourth-floor-last-door","god-shall-wipe-away-all-tears","gratitude-on-the-sabbath-day","i-will-bring-the-light-of-the-gospel-into-my-home","if-ye-had-known-me","joy-and-spiritual-survival","learn-from-alma-and-amulek","lest-thou-forget","look-to-the-book-look-to-the-lord","no-greater-joy-than-to-know-that-they-know","o-how-great-the-plan-of-our-god","principles-and-promises","repentance-a-joyful-choice","rise-up-in-strength-sisters-in-zion","serve","sharing-the-restored-gospel","that-he-may-become-strong-also","the-blessings-of-worship","the-doctrine-of-christ","the-great-plan-of-redemption","the-lord-jesus-christ-teaches-us-to-pray","the-master-healer","the-perfect-path-to-happiness","the-righteous-judge","the-sacrament-can-help-us-become-holy","the-souls-sincere-desire","there-is-power-in-the-book","to-whom-shall-we-go","valiant-in-the-testimony-of-jesus"
                }
            },
            {
                new Conference(ConferencePhase.April, 2017),
                new List<string>()
                {
                    "a-sin-resistant-generation","and-this-is-life-eternal","becoming-a-disciple-of-our-lord-jesus-christ","brighter-and-brighter-until-the-perfect-day","called-to-the-work","certain-women","confide-in-god-unwaveringly","dont-look-around-look-up","drawing-the-power-of-jesus-christ-into-our-lives","foundations-of-faith","gathering-the-family-of-god","his-daily-guiding-hand","how-does-the-holy-ghost-help-you","kindness-charity-and-love","let-the-holy-spirit-guide","my-peace-i-leave-with-you","our-fathers-glorious-plan","our-good-shepherd","overcoming-the-world","perfect-love-casteth-out-fear","prepare-the-way","return-and-receive","songs-sung-and-unsung","stand-up-inside-and-be-all-in","that-our-light-may-be-a-standard-for-the-nations","the-beauty-of-holiness","the-godhead-and-the-plan-of-salvation","the-greatest-among-you","the-language-of-the-gospel","the-power-of-the-book-of-mormon","the-voice-of-warning","then-jesus-beholding-him-loved-him","to-the-friends-and-investigators-of-the-church","trust-in-the-lord-and-lean-not","walk-with-me","whatsoever-he-saith-unto-you-do-it"
                }
            },
            {
                new Conference(ConferencePhase.October, 2017),
                new List<string>()
                {
                    "a-yearning-for-home","abiding-in-god-and-repairing-the-breach","apart-but-still-one","be-ye-therefore-perfect-eventually","bearers-of-heavenly-light","by-divine-design","do-we-trust-him-hard-is-good","earning-the-trust-of-the-lord-and-your-family","essential-truths-our-need-to-act","exceeding-great-and-precious-promises","fear-not-to-do-good","gods-compelling-witness-the-book-of-mormon","has-the-day-of-miracles-ceased","i-have-a-work-for-thee","lord-wilt-thou-cause-that-my-eyes-may-be-opened","love-one-another-as-he-has-loved-us","repentance-is-always-positive","seek-ye-out-of-the-best-books","spiritual-eclipse","that-your-joy-might-be-full","the-book-of-mormon-what-would-your-life-be-like-without-it","the-eternal-everyday","the-heart-of-the-widow","the-living-bread-which-came-down-from-heaven","the-lord-leads-his-church","the-needs-before-us","the-plan-and-the-proclamation","the-priesthood-and-the-saviors-atoning-power","the-trek-continues","the-truth-of-all-things","the-voice-of-the-lord","three-sisters","turn-on-your-light","turn-to-the-lord","value-beyond-measure"
                }
            },
            {
                new Conference(ConferencePhase.April, 2018),
                new List<string>()
                {
                    "am-i-a-child-of-god","be-with-and-strengthen-them","behold-a-royal-army","behold-the-man","christ-the-lord-is-risen-today","even-as-christ-forgives-you-so-also-do-ye","family-history-and-temple-work-sealing-and-healing","he-that-shall-endure-unto-the-end-the-same-shall-be-saved","his-spirit-to-be-with-you","inspired-ministering","introductory-remarks","it-is-all-about-people","let-us-all-press-on","meek-and-lowly-of-heart","ministering-as-the-savior-does","ministering-with-the-power-and-authority-of-god","ministering","one-more-day","precious-gifts-from-god","prepare-to-meet-god","prophets-speak-by-the-power-of-the-holy-spirit","pure-love-the-true-sign-of-every-true-disciple-of-jesus-christ","revelation-for-the-church-revelation-for-our-lives","saving-ordinances-will-bring-us-marvelous-light","small-and-simple-things","take-the-holy-spirit-as-your-guide","teaching-in-the-home-a-joyful-and-sacred-responsibility","the-elders-quorum","the-heart-of-a-prophet","the-powers-of-the-priesthood","the-prophet-of-god","until-seventy-times-seven","what-every-aaronic-priesthood-holder-needs-to-understand","with-one-accord","young-women-in-the-work"
                }
            },
            {
                new Conference(ConferencePhase.October, 2018),
                new List<string>()
                {
                    "all-must-take-upon-them-the-name-given-of-the-father","be-not-troubled","becoming-a-shepherd","becoming-exemplary-latter-day-saints","believe-love-do","choose-you-this-day","come-listen-to-a-prophets-voice","deep-and-lasting-conversion-to-heavenly-father-and-the-lord-jesus-christ","divine-discontent","firm-and-steadfast-in-the-faith-of-christ","for-him","gather-together-in-one-all-things-in-christ","laying-the-foundation-of-a-great-work","lift-up-your-head-and-rejoice","now-is-the-time","one-in-christ","opening-remarks","our-campfire-of-faith","parents-and-children","shepherding-souls","sisters-participation-in-the-gathering-of-israel","taking-upon-ourselves-the-name-of-jesus-christ","the-correct-name-of-the-church","the-father","the-joy-of-unselfish-service","the-ministry-of-reconciliation","the-role-of-the-book-of-mormon-in-conversion","the-vision-of-the-redemption-of-the-dead","truth-and-the-plan","try-try-try","wilt-thou-be-made-whole","women-and-gospel-learning-in-the-home","wounded"
                }
            },
            {
                new Conference(ConferencePhase.April, 2019),
                new List<string>()
                {
                    "11soares","12craven","13hales","14uchtdorf","15waddell","16eyring","23ballard","24held","25andersen","26wada","27homer","28holland","31stevenson","32cook","33clark","34eyring","35oaks","36nelson","41renlund","42eubank","43cook","44christofferson","45callister","46nelson","51oaks","52villar","53gong","54bednar","55mckay","56rasband","57nelson"
                }
            },
            {
                new Conference(ConferencePhase.October, 2019),
                new List<string>()
                {
                    "11holland","12vinson","13owen","14christofferson","15craig","16renlund","17oaks","22bednar","23alliaud","24nelson","25cook","26pace","27budge","28alvarado","29rasband","31aburto","32harkness","33cordon","34eyring","35oaks","36nelson","41gong","42franco","43uchtdorf","44gonzalez","45stevenson","46nelson","51eyring","52boom","53ballard","54johnson","55soares","56andersen","57nelson"
                }
            },
            {
                new Conference(ConferencePhase.April, 2020),
                new List<string>()
                {
                    "11nelson","12ballard","13rasband","14jones","15andersen","16holmes","17eyring","23soares","24mccune","25causse","26renlund","27tai","28stevenson","31gong","32alvarez","33petelo","34bingham","35eyring","36oaks","37nelson","41rasband","42cordon","43holland","44bednar","45nelson","51oaks","52cook","53gimenez","54uchtdorf","55clayton","56christofferson","57nelson"
                }
            },
            {
                new Conference(ConferencePhase.October, 2020),
                new List<string>()
                {
                    "11nelson","12bednar","13whiting","14craig","15cook","16rasband","17oaks","22christofferson","23lund","24gong","25waddell","26holland","27jackson","28uchtdorf","31eubank","32craven","34franco","35eyring","36oaks","37nelson","41ballard","42harkness","43soares","44godoy","45andersen","46nelson","51eyring","52jaggi","53stevenson","54camargo","55renlund","56johnson","57holland","58nelson"
                }
            },
            {
                new Conference(ConferencePhase.April, 2021),
                new List<string>()
                {
                    "11nelson","12uchtdorf","13jones","14newman","15stevenson","16gong","17eyring","23holland","24becerra","25renlund","26andersen","27mutombo","28ballard","31cook","32corbitt","33nielsen","34eyring","35oaks","36nelson","41soares","42aburto","43palmer","44dube","45teixeira","46wakolo","47wong","48teh","49nelson","51oaks","52rasband","53dyches","54christofferson","55walker","56bednar","57nelson"
                }
            },
            {
                new Conference(ConferencePhase.October, 2021),
                new List<string>()
                {
                    "11nelson","12holland","13cordon","14soares","15christofferson","16gilbert","17giuffra","18oaks","22bednar","23schmeil","24porter","25kopischke","26rasband","27golden","28villanueva","29stevenson","31ballard","32eubank","33nielson","34valenzuela","35wilcox","36kyungu","37nash","38eyring","41uchtdorf","42johnson","43renlund","44sikahema","46cook","47nelson","51gong","52budge","53perkins","54dunn","55douglas","56revillo","57meredith","58andersen","59nelson"
                }
            },
            {
                new Conference(ConferencePhase.April, 2022),
                new List<string>()
                {
                    "11nelson","12ballard","13aburto","14bednar","15andersen","16gavarret","17kacher","18eyring","23holland","24kearon","25aidukaitis","26gong","27ochoa","28hamilton","29cook","31oaks","32porter","33craven","35bingham","36renlund","41christofferson","42wright","43stevenson","44ringwood","45rasband","46martinez","47nelson","51oaks","52ojediran","53klebingat","54pace","55soares","56funk","57uchtdorf","58nelson"
                }
            },
            {
                new Conference(ConferencePhase.October, 2022),
                new List<string>()
                {
                    "12uchtdorf","13browning","14renlund","15pino","16montoya","17rasband","18oaks","19nelson","22ballard","23yee","24johnson","25soares","26mcconkie","27zeballos","28christofferson","31causse","32craig","33pearson","34silva","35andersen","41holland","42dennis","43gong","44sitati","45lund","46bednar","47nelson","51eyring","52olsen","53schmitt","54eddy","55stevenson","56morrison","57cook","58nelson"
                }
            },
            {
                new Conference(ConferencePhase.April, 2023),
                new List<string>()
                {
                    "11stevenson","12cordon","13cook","14gong","15cook","16haynie","17eyring","23renlund","24meurs","25bennett","26christensen","27schmutz","28de-hoyos","29uchtdorf","31bragg","32camargo","33nattress","34uceda","41christofferson","42johnson","43soares","44yamashita","45andersen","46duncan","47nelson","51oaks","52ballard","53rasband","54stanfill","55bassett","56corbitt","57bednar","58nelson"
                }
            },
            {
                new Conference(ConferencePhase.October, 2023),
                new List<string>()
                {
                    "11bednar","12wright","13daines","14godoy","15christofferson","16ardern","17oaks","22andersen","23newman","24costa","25stevenson","26choi","27phillips","28rasband","31sabin","32koch","33runia","34soares","41ballard","42freeman","43parrella","44cook","45uchtdorf","46waddell","47eyring","51nelson","52pingree","53cordon","54gong","55esplin","56giraud-carrier","57renlund"
                }
            },
            {
                new Conference(ConferencePhase.April, 2024),
                new List<string>()
                {
                    "13holland","14dennis","15dushku","16soares","17gerard","18eyring","21bednar","22de-feo","23nielson","24alonso","25gong","26nelson","27cook","31bowen","32bangerter","33spannaus","34carpenter","35uchtdorf","41rasband","42porter","43renlund","44pieper","45kearon","46taylor","47oaks","51christofferson","52godoy","53stevenson","54held","55andersen","56pace","57nelson"
                }
            },
            {
                new Conference(ConferencePhase.October, 2024),
                new List<string>()
                {
                    "12andersen","13freeman","14hirst","15renlund","16homer","17casillas","18oaks","21christofferson","22teixeira","23villar","24kearon","25buckner","26goury","27cavalcante","28soares","31gong","32yee","33mckay","34alvarado","35bednar","41holland","42browning","43hales","44stevenson","45budge","46wilcox","47eyring","51uchtdorf","52wada","53rasband","54cook","55alliaud","56egbo","57nelson"
                }
            },
            {
                new Conference(ConferencePhase.April, 2025),
                new List<string>()
                {
                    "13holland","14johnson","15rasband","16cook","17gimenez","18eyring","21andersen","22lund","23palmer","24roman","25renlund","26boom","27uchtdorf","31stevenson","32wright","33rasband","34vargas","35christofferson","41bednar","42shumway","43runia","44causse","45gong","46mccune","47oaks","51soares","52strong","53whiting","54kim","55kearon","56tai","57nelson"
                }
            },
            {
                new Conference(ConferencePhase.October, 2025),
                new List<string>()
                {
                    "12stevenson","13browning","14barcellos","15eyre","16uchtdorf","17johnson","19oaks","21rasband","22webb","23jaggi","24brown","25gong","26cziesla","27cook","31kearon","32dennis","33barlow","34jackson","35andersen","41holland","42evanson","43soares","44johnson","45christofferson","46spannaus","47eyring","51bednar","52cuvelier","53holland","54godoy","55renlund","56amos","57farias","58oaks"
                }
            }
        };
    }
}
