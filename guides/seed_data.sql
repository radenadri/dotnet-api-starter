--
-- PostgreSQL database dump
--

\restrict 7qgecwCruezvdv3b09df168jdpxbnap012ThSN2AdC43ObONEC8wqua0yx2r9lK

-- Dumped from database version 17.6 (Homebrew)
-- Dumped by pg_dump version 17.6 (Homebrew)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."__EFMigrationsHistory" VALUES ('20261030191304_InitialCreate', '8.0.8');
INSERT INTO public."__EFMigrationsHistory" VALUES ('20261031152726_SyncBookshelfAndRealWorldUsers', '8.0.8');


--
-- Data for Name: authors; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.authors VALUES (1, 'Ayana Elizabeth Johnson', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (2, 'Barack Obama', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (3, 'Beth Duke', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (4, 'Dean Koontz', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (5, 'George Orwell', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (6, 'Harper Lee', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (7, 'Ina Garten', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (8, 'Jacob Goldstein', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (9, 'Jeanine Cummins', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (10, 'Katherine K. Wilkinson', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (11, 'Lewis Carroll', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (12, 'Lysa TerKeurst', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (13, 'Mark Manson', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (14, 'Peter C. Brown', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (15, 'Philip Freeman', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (16, 'Richard H. Thaler', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (17, 'Stephen Hawking', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');
INSERT INTO public.authors VALUES (18, 'Tapan Gupta', '2025-10-20 01:38:28.264088', '2025-10-20 01:38:28.264088');


--
-- Data for Name: formats; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.formats VALUES (1, 'Hardcover', '2025-10-20 01:38:28.264914', '2025-10-20 01:38:28.264914');
INSERT INTO public.formats VALUES (2, 'Paperback', '2025-10-20 01:38:28.264914', '2025-10-20 01:38:28.264914');


--
-- Data for Name: publishers; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.publishers VALUES (1, 'Belknap Press', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (2, 'Clarkson Potter', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (3, 'Crown', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (4, 'Grand Central Publishing', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (5, 'Harper', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (6, 'Harper Paperbacks', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (7, 'Holt Paperbacks', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (8, 'Independent', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (9, 'One World', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (10, 'Paw Prints', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (11, 'Penguin Books', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (12, 'Peter Pauper Press', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (13, 'Simon & Schuster', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (14, 'Springer', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (15, 'The Art of Dixie', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (16, 'Thomas & Mercer', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');
INSERT INTO public.publishers VALUES (17, 'Thomas Nelson', '2025-10-20 01:38:28.264593', '2025-10-20 01:38:28.264593');


--
-- Data for Name: books; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.books VALUES (1, 'To Kill a Mockingbird', 'Scout Finch lives with her brother, Jem, and their widowed father, Atticus, in the sleepy Alabama town of Maycomb. Maycomb is suffering through the Great Depression, but Atticus is a prominent lawyer and the Finch family is reasonably well off in comparison to the rest of society. One summer, Jem and Scout befriend a boy named Dill, who has come to live in their neighborhood for the summer, and the trio acts out stories together. Eventually, Dill becomes fascinated with the spooky house on their street called the Radley Place. The house is owned by Mr. Nathan Radley, whose brother, Arthur (nicknamed Boo), has lived there for years without venturing outside.', NULL, 'English', '0062420704', '978-0062420701', 281, 2015, 5.0, 'To read', 0, NULL, NULL, NULL, NULL, 5, 2, '2025-10-20 01:38:28.265504', '2025-10-20 01:38:28.265504');
INSERT INTO public.books VALUES (2, 'A Brief History of Time', 'A Brief History of Time is a book by Stephen Hawking that was first published in 1988...', NULL, 'English', '1439503923', '978-1439503928', 256, 2008, 5.0, 'To read', 0, NULL, NULL, NULL, NULL, 10, 2, '2025-10-20 01:38:28.267104', '2025-10-20 01:38:28.267104');
INSERT INTO public.books VALUES (3, 'All We Can Save', 'There is a renaissance blooming in the climate movement...', NULL, 'English', '1441344950', '978-1441344953', 448, 2021, 4.0, 'To read', 0, NULL, NULL, NULL, NULL, 9, 2, '2025-10-20 01:38:28.267669', '2025-10-20 01:38:28.267669');
INSERT INTO public.books VALUES (4, 'Alice in Wonderland', 'Step into the poetical, puzzling, wonderful worlds of Alice''s imagination...', NULL, 'English', '1441344950', '978-1441344953', 240, 2024, 4.0, 'To read', 0, NULL, NULL, NULL, NULL, 12, 2, '2025-10-20 01:38:28.268726', '2025-10-20 01:38:28.268726');
INSERT INTO public.books VALUES (5, 'Money : The True Story of a Made-Up Thing', 'Money only works because we all agree to believe in it...', NULL, 'English', '0316417203', '978-0316417204', 272, 2022, 2.0, 'To read', 0, NULL, NULL, NULL, NULL, 4, 2, '2025-10-20 01:38:28.269677', '2025-10-20 01:38:28.269677');
INSERT INTO public.books VALUES (6, 'Modern Comfort Food', 'In Modern Comfort Food, Ina Garten shares 85 new recipes...', NULL, 'English', '0804187061', '978-0804187060', 256, 2020, 3.0, 'To read', 0, NULL, NULL, NULL, NULL, 2, 2, '2025-10-20 01:38:28.270638', '2025-10-20 01:38:28.270638');
INSERT INTO public.books VALUES (7, 'Nudge', 'Every day we make choices—about what to buy or eat...', NULL, 'English', '014311526X', '978-0143115267', 312, 2009, 4.0, 'To read', 0, NULL, NULL, NULL, NULL, 11, 2, '2025-10-20 01:38:28.271603', '2025-10-20 01:38:28.271603');
INSERT INTO public.books VALUES (8, 'American Dirt', 'Lydia lives in Acapulco. She has a son, Luca...', NULL, 'English', '1250209781', '978-1250209788', 400, 2022, 5.0, 'To read', 0, NULL, NULL, NULL, NULL, 7, 2, '2025-10-20 01:38:28.272334', '2025-10-20 01:38:28.272334');
INSERT INTO public.books VALUES (9, 'Copper Interconnect Technology', 'Since overall circuit performance has depended primarily on transistor properties...', NULL, 'English', '1441900756', '978-1441900753', 442, 2009, 4.0, 'To read', 0, NULL, NULL, NULL, NULL, 14, 2, '2025-10-20 01:38:28.273518', '2025-10-20 01:38:28.273518');
INSERT INTO public.books VALUES (10, 'It All Comes Back To You', 'In 2012, she''s still beautiful, charming, and surrounded by admirers...', NULL, 'English', '0578448831', '978-0578448831', 294, 2018, 5.0, 'To read', 0, NULL, NULL, NULL, NULL, 15, 2, '2025-10-20 01:38:28.274257', '2025-10-20 01:38:28.274257');
INSERT INTO public.books VALUES (11, 'Make It Stick : The Science of Successful Learning', 'Are you tired of forgetting what you learn? This groundbreaking book...', NULL, 'English', '0674729013', '978-0674729018', 336, 2014, 4.0, 'To read', 0, NULL, NULL, NULL, NULL, 1, 2, '2025-10-20 01:38:28.275175', '2025-10-20 01:38:28.275175');
INSERT INTO public.books VALUES (12, 'Elsewhere', 'Since his wife, Michelle, left seven years ago...', NULL, 'English', '1542019877', '978-1542019873', 367, 2021, 3.0, 'To read', 0, NULL, NULL, NULL, NULL, 16, 2, '2025-10-20 01:38:28.276148', '2025-10-20 01:38:28.276148');
INSERT INTO public.books VALUES (13, 'Alexander The Great', 'The celebrated Macedonian king has been one of the most enduring figures in history...', NULL, 'English', '1416592814', '978-1416592815', 424, 2011, 3.0, 'To read', 0, NULL, NULL, NULL, NULL, 13, 1, '2025-10-20 01:38:28.277353', '2025-10-20 01:38:28.277353');
INSERT INTO public.books VALUES (14, 'Animal Farm', 'Animal Farm is an allegorical novella by George Orwell...', NULL, 'English', '', '979-8308648819', 111, 2025, 4.0, 'To read', 0, NULL, NULL, NULL, NULL, 8, 2, '2025-10-20 01:38:28.278242', '2025-10-20 01:38:28.278242');
INSERT INTO public.books VALUES (15, 'A Promised Land', 'In the stirring first volume of his presidential memoirs, Barack Obama tells the story...', NULL, 'English', '1524763160', '978-1524763169', 768, 2020, 4.0, 'To read', 0, NULL, NULL, NULL, NULL, 3, 2, '2025-10-20 01:38:28.279277', '2025-10-20 01:38:28.279277');
INSERT INTO public.books VALUES (16, 'Everything is F*cked', 'We live in an interesting time. Materially, everything is the best it’s ever been...', NULL, 'English', '0062956566', '978-0062956569', 288, 2020, 5.0, 'To read', 0, NULL, NULL, NULL, NULL, 6, 2, '2025-10-20 01:38:28.280423', '2025-10-20 01:38:28.280423');
INSERT INTO public.books VALUES (17, 'It’s Not Supposed To Be This Way', 'Life often looks so very different than we hoped or expected...', NULL, 'English', '0718039858', '978-0718039851', 256, 2018, 4.0, 'To read', 0, NULL, NULL, NULL, NULL, 17, 2, '2025-10-20 01:38:28.28138', '2025-10-20 01:38:28.28138');


--
-- Data for Name: books_authors; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.books_authors VALUES (1, 6, '2025-10-20 01:38:28.266121', '2025-10-20 01:38:28.266121');
INSERT INTO public.books_authors VALUES (2, 17, '2025-10-20 01:38:28.267286', '2025-10-20 01:38:28.267286');
INSERT INTO public.books_authors VALUES (3, 1, '2025-10-20 01:38:28.267925', '2025-10-20 01:38:28.267925');
INSERT INTO public.books_authors VALUES (3, 10, '2025-10-20 01:38:28.268162', '2025-10-20 01:38:28.268162');
INSERT INTO public.books_authors VALUES (4, 11, '2025-10-20 01:38:28.268978', '2025-10-20 01:38:28.268978');
INSERT INTO public.books_authors VALUES (5, 8, '2025-10-20 01:38:28.269931', '2025-10-20 01:38:28.269931');
INSERT INTO public.books_authors VALUES (6, 7, '2025-10-20 01:38:28.270887', '2025-10-20 01:38:28.270887');
INSERT INTO public.books_authors VALUES (7, 16, '2025-10-20 01:38:28.271876', '2025-10-20 01:38:28.271876');
INSERT INTO public.books_authors VALUES (8, 9, '2025-10-20 01:38:28.272573', '2025-10-20 01:38:28.272573');
INSERT INTO public.books_authors VALUES (9, 18, '2025-10-20 01:38:28.273763', '2025-10-20 01:38:28.273763');
INSERT INTO public.books_authors VALUES (10, 3, '2025-10-20 01:38:28.274498', '2025-10-20 01:38:28.274498');
INSERT INTO public.books_authors VALUES (11, 14, '2025-10-20 01:38:28.275419', '2025-10-20 01:38:28.275419');
INSERT INTO public.books_authors VALUES (12, 4, '2025-10-20 01:38:28.276369', '2025-10-20 01:38:28.276369');
INSERT INTO public.books_authors VALUES (13, 15, '2025-10-20 01:38:28.277708', '2025-10-20 01:38:28.277708');
INSERT INTO public.books_authors VALUES (14, 5, '2025-10-20 01:38:28.278499', '2025-10-20 01:38:28.278499');
INSERT INTO public.books_authors VALUES (15, 2, '2025-10-20 01:38:28.279666', '2025-10-20 01:38:28.279666');
INSERT INTO public.books_authors VALUES (16, 13, '2025-10-20 01:38:28.280752', '2025-10-20 01:38:28.280752');
INSERT INTO public.books_authors VALUES (17, 12, '2025-10-20 01:38:28.281762', '2025-10-20 01:38:28.281762');


--
-- Data for Name: categories; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.categories VALUES (1, 'Allegory', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (2, 'Autobiography', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (3, 'Bildungsroman', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (4, 'Biography', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (5, 'Business', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (6, 'Children''s Books', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (7, 'Comedy', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (8, 'Cooking', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (9, 'Economics', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (10, 'Fantasy', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (11, 'Fiction', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (12, 'Food', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (13, 'History', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (14, 'Humor', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (15, 'Mystery', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (16, 'Non-Fiction', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (17, 'Novel', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (18, 'Poetry', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (19, 'Satire', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (20, 'Science', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (21, 'Science Fiction', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (22, 'Self-Help', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (23, 'Southern Gothic', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (24, 'Study Guide', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');
INSERT INTO public.categories VALUES (25, 'Thriller', '2025-10-20 01:38:28.265193', '2025-10-20 01:38:28.265193');


--
-- Data for Name: books_categories; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.books_categories VALUES (1, 17, '2025-10-20 01:38:28.266428', '2025-10-20 01:38:28.266428');
INSERT INTO public.books_categories VALUES (1, 23, '2025-10-20 01:38:28.266742', '2025-10-20 01:38:28.266742');
INSERT INTO public.books_categories VALUES (1, 3, '2025-10-20 01:38:28.26694', '2025-10-20 01:38:28.26694');
INSERT INTO public.books_categories VALUES (2, 21, '2025-10-20 01:38:28.267446', '2025-10-20 01:38:28.267446');
INSERT INTO public.books_categories VALUES (3, 18, '2025-10-20 01:38:28.268446', '2025-10-20 01:38:28.268446');
INSERT INTO public.books_categories VALUES (4, 6, '2025-10-20 01:38:28.269214', '2025-10-20 01:38:28.269214');
INSERT INTO public.books_categories VALUES (4, 10, '2025-10-20 01:38:28.26946', '2025-10-20 01:38:28.26946');
INSERT INTO public.books_categories VALUES (5, 5, '2025-10-20 01:38:28.270178', '2025-10-20 01:38:28.270178');
INSERT INTO public.books_categories VALUES (5, 9, '2025-10-20 01:38:28.270413', '2025-10-20 01:38:28.270413');
INSERT INTO public.books_categories VALUES (6, 8, '2025-10-20 01:38:28.271139', '2025-10-20 01:38:28.271139');
INSERT INTO public.books_categories VALUES (6, 12, '2025-10-20 01:38:28.271374', '2025-10-20 01:38:28.271374');
INSERT INTO public.books_categories VALUES (7, 16, '2025-10-20 01:38:28.272102', '2025-10-20 01:38:28.272102');
INSERT INTO public.books_categories VALUES (8, 11, '2025-10-20 01:38:28.272836', '2025-10-20 01:38:28.272836');
INSERT INTO public.books_categories VALUES (8, 15, '2025-10-20 01:38:28.273061', '2025-10-20 01:38:28.273061');
INSERT INTO public.books_categories VALUES (8, 25, '2025-10-20 01:38:28.273288', '2025-10-20 01:38:28.273288');
INSERT INTO public.books_categories VALUES (9, 20, '2025-10-20 01:38:28.274031', '2025-10-20 01:38:28.274031');
INSERT INTO public.books_categories VALUES (10, 14, '2025-10-20 01:38:28.274722', '2025-10-20 01:38:28.274722');
INSERT INTO public.books_categories VALUES (10, 7, '2025-10-20 01:38:28.274947', '2025-10-20 01:38:28.274947');
INSERT INTO public.books_categories VALUES (11, 22, '2025-10-20 01:38:28.275649', '2025-10-20 01:38:28.275649');
INSERT INTO public.books_categories VALUES (11, 24, '2025-10-20 01:38:28.275879', '2025-10-20 01:38:28.275879');
INSERT INTO public.books_categories VALUES (12, 10, '2025-10-20 01:38:28.276596', '2025-10-20 01:38:28.276596');
INSERT INTO public.books_categories VALUES (12, 21, '2025-10-20 01:38:28.276876', '2025-10-20 01:38:28.276876');
INSERT INTO public.books_categories VALUES (13, 13, '2025-10-20 01:38:28.278003', '2025-10-20 01:38:28.278003');
INSERT INTO public.books_categories VALUES (14, 1, '2025-10-20 01:38:28.278767', '2025-10-20 01:38:28.278767');
INSERT INTO public.books_categories VALUES (14, 19, '2025-10-20 01:38:28.278997', '2025-10-20 01:38:28.278997');
INSERT INTO public.books_categories VALUES (15, 4, '2025-10-20 01:38:28.279966', '2025-10-20 01:38:28.279966');
INSERT INTO public.books_categories VALUES (15, 2, '2025-10-20 01:38:28.280193', '2025-10-20 01:38:28.280193');
INSERT INTO public.books_categories VALUES (16, 22, '2025-10-20 01:38:28.281099', '2025-10-20 01:38:28.281099');
INSERT INTO public.books_categories VALUES (17, 22, '2025-10-20 01:38:28.282039', '2025-10-20 01:38:28.282039');


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.users VALUES ('34697229-21b1-418b-aaaf-4586847508cc', 'brandnewshvt', '$2a$11$67DVogUkPYD.ETIagul0Xed787EurQg46NY6MVhB4X3OiTuO1tuMq', 'brandnewshvt@example.com', 'admin', true, '2026-10-31 22:31:13.809168+07', '2026-10-31 22:31:13.809168+07');
INSERT INTO public.users VALUES ('bd49481a-b11d-4dcc-a1e2-a038b626c4c3', 'ab', '$2a$11$phqEW81jeDFuYzmNEW5xFu0IpYbLbLa2BDJeMFsxnFUpcny9cqaOe', 'ab@example.com', 'user', true, '2026-10-31 22:31:13.809168+07', '2026-10-31 22:31:13.809168+07');
INSERT INTO public.users VALUES ('3164cb16-143b-443f-b444-c83e62fc97e9', 'user123', '$2a$11$BGOs6IB3UJqFzWbS1LczDOh9QC5DXjdt0.z1avnmAYwwNRKYS2Ul6', 'user123@example.com', 'user', true, '2026-10-31 22:31:13.809168+07', '2026-10-31 22:31:13.809168+07');


--
-- Data for Name: todos; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Name: Todos_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Todos_Id_seq"', 1, true);


--
-- Name: authors_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.authors_id_seq', 18, true);


--
-- Name: books_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.books_id_seq', 17, true);


--
-- Name: categories_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.categories_id_seq', 25, true);


--
-- Name: formats_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.formats_id_seq', 2, true);


--
-- Name: publishers_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.publishers_id_seq', 17, true);


--
-- PostgreSQL database dump complete
--

\unrestrict 7qgecwCruezvdv3b09df168jdpxbnap012ThSN2AdC43ObONEC8wqua0yx2r9lK

