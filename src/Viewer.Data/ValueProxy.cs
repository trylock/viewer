
using System.Globalization;

namespace Viewer.Data
{
    
    public class FormattedIntValue : IntValue
    {
        private readonly IValueFormatter<FormattedIntValue> _formatter;

        public FormattedIntValue(IntValue value, IValueFormatter<FormattedIntValue> formatter) : base(value?.Value)
        {
            _formatter = formatter;
        }

        public override string ToString()
        {
            return _formatter.Format(this);
        }
        
        public override string ToString(CultureInfo culture)
        {
            return _formatter.Format(this, culture);
        }
    }

    
    public class FormattedRealValue : RealValue
    {
        private readonly IValueFormatter<FormattedRealValue> _formatter;

        public FormattedRealValue(RealValue value, IValueFormatter<FormattedRealValue> formatter) : base(value?.Value)
        {
            _formatter = formatter;
        }

        public override string ToString()
        {
            return _formatter.Format(this);
        }
        
        public override string ToString(CultureInfo culture)
        {
            return _formatter.Format(this, culture);
        }
    }

    
    public class FormattedStringValue : StringValue
    {
        private readonly IValueFormatter<FormattedStringValue> _formatter;

        public FormattedStringValue(StringValue value, IValueFormatter<FormattedStringValue> formatter) : base(value?.Value)
        {
            _formatter = formatter;
        }

        public override string ToString()
        {
            return _formatter.Format(this);
        }
        
        public override string ToString(CultureInfo culture)
        {
            return _formatter.Format(this, culture);
        }
    }

    
    public class FormattedDateTimeValue : DateTimeValue
    {
        private readonly IValueFormatter<FormattedDateTimeValue> _formatter;

        public FormattedDateTimeValue(DateTimeValue value, IValueFormatter<FormattedDateTimeValue> formatter) : base(value?.Value)
        {
            _formatter = formatter;
        }

        public override string ToString()
        {
            return _formatter.Format(this);
        }
        
        public override string ToString(CultureInfo culture)
        {
            return _formatter.Format(this, culture);
        }
    }

    
    public class FormattedImageValue : ImageValue
    {
        private readonly IValueFormatter<FormattedImageValue> _formatter;

        public FormattedImageValue(ImageValue value, IValueFormatter<FormattedImageValue> formatter) : base(value?.Value)
        {
            _formatter = formatter;
        }

        public override string ToString()
        {
            return _formatter.Format(this);
        }
        
        public override string ToString(CultureInfo culture)
        {
            return _formatter.Format(this, culture);
        }
    }

    }