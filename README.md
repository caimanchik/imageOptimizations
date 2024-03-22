# Домашнее задание
## Основное задание
Вам дан код JPEG подобного сжатия (проект JPEG), вам нужно максимально, насколько это возможно, оптимизировать его, в том числе уменьшить потребление памяти. ВАЖНО: нужно именно оптимизировать данное решение а не переписывать его с 0!

Рекомендации:
* Профилируйте код (используйте dotTrace)
* Для начала оптимизируйте загрузку изображений и переписывайте только неэффективный код
* Пишите бенчмарки на разные методы
* Не бойтесь математики

С разными вопросами можно писать @Golrans и @ryzhes

Подсказки:
* Распаралельте DCT
* CbCr subsampling
* Используйте указатели, вместо GetPixel/SetPixel, придётся написать unsafe код
* Замените DCT на FFT (System.Numerics.Complex), нельзя использовать библиотеки, только собственная реализация!
* Помимо подсказанного в проекте ещё много узких мест (╯°□°）╯︵ ┻━┻

Как сдавать задание:
1. Нужно сделать замер через JpegProcessorBenchmark до оптимизаций и запомнить Mean и Allocated по операциям Compress и Uncompress
2. Сделать аналогичные замеры после оптимизаций
3. Внести свой результат в таблицу в день дедлайна, ссылку на которую вам дадут позже
4. Очно или онлайн за 10-15 минут рассказать какие моменты удалось найти и как оптимизировать

Дополнительное задание:
В проекте [debugging/managed/Volatile](https://github.com/Golran/shpora-debug-optimizations-2024/tree/master/debugging/managed/Volatile)
запустить программу в Debug и Release режиме. В чём разница между запусками? Используйте windbg, посмотрите ассемблерный код программы и объясните, что происходит.

## Полезные ссылки
* [Про new()](https://devblogs.microsoft.com/premier-developer/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/)
* [Про IEquatable](https://devblogs.microsoft.com/premier-developer/performance-implications-of-default-struct-equality-in-c/)
* [Про Inlining методов](https://web.archive.org/web/20200108171755/http://blogs.microsoft.co.il/sasha/2012/01/20/aggressive-inlining-in-the-clr-45-jit/)
* [Habr cтатья про for vs foreach](https://habr.com/ru/companies/skbkontur/articles/743454/)
* [Презентация оптимизация](https://docs.google.com/presentation/d/1HdFkuCcFGV3G8uXo5hjjTAsmDNv6_0o6WbdxZklUJkk/edit?usp=sharing)


* [WinDbg commands](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/commands)
* [sos commands](http://www.windbg.xyz/windbg/article/10-SOS-Extension-Commands)
* [sosex commands](https://knowledge-base.havit.eu/tag/windbg/)
* [Презентация отладка](https://docs.google.com/presentation/d/1Hq5osHm565PwxnMr1TE8Iy99K_4OWws7VpS-Jel7nZ4/edit?usp=sharing)
