document.addEventListener('alpine:init', () => {
    Alpine.data('datetimePicker', (opts = {}) => {
        const MONTHS = ['Ocak','Şubat','Mart','Nisan','Mayıs','Haziran','Temmuz','Ağustos','Eylül','Ekim','Kasım','Aralık'];
        const DAY_NAMES = ['Pt','Sa','Ça','Pe','Cu','Ct','Pz'];

        let initY, initMo, initD, initH = 9, initMi = 0;
        const now = new Date();
        const hasInitialValue = !!(opts.value && /^\d{4}-\d{2}-\d{2}/.test(opts.value));
        if (hasInitialValue) {
            const [datePart, timePart] = opts.value.split('T');
            const [y, mo, d] = datePart.split('-').map(Number);
            initY = y; initMo = mo - 1; initD = d;
            if (timePart) {
                const [h, mi] = timePart.split(':').map(Number);
                initH = isNaN(h) ? 9 : h;
                initMi = isNaN(mi) ? 0 : mi;
            }
        } else {
            initY = now.getFullYear(); initMo = now.getMonth(); initD = now.getDate();
        }

        return {
            open: false,
            view: 'day',
            hasValue: hasInitialValue,
            mode: opts.mode || 'datetime',
            field: opts.field || '',
            year: initY, month: initMo, day: initD,
            hour: initH, minute: initMi,
            viewYear: initY, viewMonth: initMo,
            yearRangeStart: Math.floor(initY / 12) * 12,
            months: MONTHS, dayNames: DAY_NAMES,

            get isoValue() {
                if (!this.hasValue) return '';
                const y = this.year;
                const m = String(this.month + 1).padStart(2, '0');
                const d = String(this.day).padStart(2, '0');
                if (this.mode === 'date') return `${y}-${m}-${d}`;
                const h = String(this.hour).padStart(2, '0');
                const mi = String(this.minute).padStart(2, '0');
                return `${y}-${m}-${d}T${h}:${mi}`;
            },

            get displayValue() {
                if (!this.hasValue) return '';
                const y = this.year;
                const m = String(this.month + 1).padStart(2, '0');
                const d = String(this.day).padStart(2, '0');
                if (this.mode === 'date') return `${d}.${m}.${y}`;
                const h = String(this.hour).padStart(2, '0');
                const mi = String(this.minute).padStart(2, '0');
                return `${d}.${m}.${y} ${h}:${mi}`;
            },

            get headerLabel() {
                if (this.view === 'year') return `${this.yearRangeStart} – ${this.yearRangeStart + 11}`;
                if (this.view === 'month') return String(this.viewYear);
                return this.months[this.viewMonth] + ' ' + this.viewYear;
            },

            get headerClickable() {
                return this.view !== 'year';
            },

            get calendarDays() {
                const firstDay = new Date(this.viewYear, this.viewMonth, 1);
                let dow = firstDay.getDay();
                dow = dow === 0 ? 6 : dow - 1;

                const lastDate = new Date(this.viewYear, this.viewMonth + 1, 0).getDate();
                const today = new Date();
                const todayKey = `${today.getFullYear()}-${today.getMonth()}-${today.getDate()}`;
                const selKey = `${this.year}-${this.month}-${this.day}`;

                const days = [];

                for (let i = dow - 1; i >= 0; i--) {
                    const d = new Date(this.viewYear, this.viewMonth, -i);
                    const k = `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
                    days.push({ n: d.getDate(), mo: d.getMonth(), yr: d.getFullYear(), cur: false, today: k === todayKey, sel: k === selKey });
                }

                for (let i = 1; i <= lastDate; i++) {
                    const k = `${this.viewYear}-${this.viewMonth}-${i}`;
                    days.push({ n: i, mo: this.viewMonth, yr: this.viewYear, cur: true, today: k === todayKey, sel: k === selKey });
                }

                let n = 1;
                while (days.length < 42) {
                    const d = new Date(this.viewYear, this.viewMonth + 1, n++);
                    const k = `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
                    days.push({ n: d.getDate(), mo: d.getMonth(), yr: d.getFullYear(), cur: false, today: k === todayKey, sel: k === selKey });
                }

                return days;
            },

            get calendarYears() {
                return Array.from({ length: 12 }, (_, i) => this.yearRangeStart + i);
            },

            prevNav() {
                if (this.view === 'day') {
                    if (this.viewMonth === 0) { this.viewYear--; this.viewMonth = 11; }
                    else this.viewMonth--;
                } else if (this.view === 'month') {
                    this.viewYear--;
                } else {
                    this.yearRangeStart -= 12;
                }
            },

            nextNav() {
                if (this.view === 'day') {
                    if (this.viewMonth === 11) { this.viewYear++; this.viewMonth = 0; }
                    else this.viewMonth++;
                } else if (this.view === 'month') {
                    this.viewYear++;
                } else {
                    this.yearRangeStart += 12;
                }
            },

            headerClick() {
                if (this.view === 'day') this.view = 'month';
                else if (this.view === 'month') this.view = 'year';
            },

            selectMonth(mo) {
                this.viewMonth = mo;
                this.view = 'day';
            },

            selectYear(yr) {
                this.viewYear = yr;
                this.yearRangeStart = Math.floor(yr / 12) * 12;
                this.view = 'month';
            },

            selectDay(d) {
                this.year = d.yr; this.month = d.mo; this.day = d.n;
                this.hasValue = true;
                if (this.mode === 'date') this.open = false;
                this.emit();
            },

            clearValue() {
                this.hasValue = false;
                this.open = false;
                this.emit();
            },

            adjustHour(delta) {
                this.hour = (this.hour + delta + 24) % 24;
                this.emit();
            },

            adjustMinute(delta) {
                this.minute = (this.minute + delta + 60) % 60;
                this.emit();
            },

            onHourInput(e) {
                const n = parseInt(e.target.value);
                this.hour = isNaN(n) ? this.hour : Math.min(23, Math.max(0, n));
                e.target.value = String(this.hour).padStart(2, '0');
                this.emit();
            },

            onMinuteInput(e) {
                const n = parseInt(e.target.value);
                this.minute = isNaN(n) ? this.minute : Math.min(59, Math.max(0, n));
                e.target.value = String(this.minute).padStart(2, '0');
                this.emit();
            },

            confirm() {
                this.open = false;
                this.emit();
            },

            toggle() {
                if (!this.open) {
                    this.viewYear = this.year;
                    this.viewMonth = this.month;
                    this.yearRangeStart = Math.floor(this.year / 12) * 12;
                    this.view = 'day';
                }
                this.open = !this.open;
            },

            setValue(valStr) {
                if (valStr && /^\d{4}-\d{2}-\d{2}/.test(valStr)) {
                    const [datePart, timePart] = valStr.split('T');
                    const [y, mo, d] = datePart.split('-').map(Number);
                    this.year = y; this.month = mo - 1; this.day = d;
                    this.viewYear = y; this.viewMonth = mo - 1;
                    if (timePart) {
                        const [h, mi] = timePart.split(':').map(Number);
                        this.hour = isNaN(h) ? 9 : h;
                        this.minute = isNaN(mi) ? 0 : mi;
                    }
                    this.hasValue = true;
                } else {
                    this.hasValue = false;
                }
            },

            emit() {
                this.$dispatch('datechanged', { value: this.isoValue, field: this.field });
            }
        };
    });
});
