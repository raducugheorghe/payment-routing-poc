## Propunere PoC: Payment Routing & Resiliency Engine
Ca o continuare a discutiei noastre despre noua arhitectura si tranzitia catre .NET si DDD, ne-ar placea sa facem un mic exercitiu practic.
Scopul nu este un sistem complet pentru productie, ci un Proof of Concept (PoC) prin care sa vedem cum abordezi design-ul de sistem si scrierea codului intr-un context de Payment Orchestration.

## Scenariul de Business:
Vrem sa expunem un API care primeste o cerere de plata (ex: Suma, Moneda, Metoda de Plata, MerchantId). Sistemul trebuie sa ruteze aceasta plata catre un Payment Service Provider (PSP) principal. Daca PSP-ul principal esueaza sau da timeout, sistemul trebuie sa faca un fallback automat catre un PSP secundar, pentru a salva tranzactia.

## Cerinte Tehnice & Livrabile:
1. API Endpoint: Un endpoint simplu de POST /api/payments care accepta payload-ul de plata.
2. Abordare DDD: Incearca sa structurezi codul folosind concepte de Domain-Driven Design (ex: separarea pe layere, un Agregat Payment care sa isi gestioneze propriile stari: Pending, Processed, Failed).
3. Simulare PSP: Creeaza doua servicii "dummy" (mock) pentru PSP1 si PSP2. Simuleaza o intarziere (delay) sau o eroare aleatoare in PSP1 pentru a declansa mecanismul de fallback catre PSP2.
4. Rezilienta: Foloseste un pattern sau o librarie la alegerea ta (ex. Polly) pentru a gestiona retry-urile sau fallback-ul (orice mecanism).
5. Stocare In-Memory: Pentru a nu pierde timp cu setup-ul bazei de date, poti folosi Entity Framework Core In-Memory sau o simpla stocare concurenta in memorie pentru a salva starea platii.
6. Teste: Adauga 1-2 teste unitare relevante pentru a valida logica de business (ex: testarea flow-ului de fallback).

## Bonus (Optional / Extra Mile) - Domain Events:
Dupa ce plata este procesata cu succes sau esueaza definitiv, genereaza un eveniment de domeniu (ex. PaymentSucceededEvent sau PaymentFailedEvent).
Foloseste un mediator in-memory (precum MediatR) pentru a publica acest eveniment.
Creeaza un handler separat care sa asculte evenimentul si sa simuleze o actiune decuplata (de exemplu, simpla logare in consola a unui mesaj de tipul "Trimitere notificare catre client pentru plata X").

## Info
Ne intereseaza calitatea codului, deciziile arhitecturale si modul in care gestionezi logica de domeniu, nu un boilerplate masiv.
Cand esti gata, ne poti lasa un link catre un repository GitHub sau o arhiva, alaturi de un scurt fisier Readme cu instructiuni de rulare. 