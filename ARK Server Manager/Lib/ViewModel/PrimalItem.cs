﻿using System;
using System.Windows;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class PrimalItem : DependencyObject
    {
        public static readonly DependencyProperty ArkApplicationProperty = DependencyProperty.Register(nameof(ArkApplication), typeof(ArkApplication), typeof(PrimalItem), new PropertyMetadata(ArkApplication.SurvivalEvolved));
        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(nameof(ClassName), typeof(string), typeof(PrimalItem), new PropertyMetadata(String.Empty));
        public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(nameof(Category), typeof(string), typeof(PrimalItem), new PropertyMetadata(String.Empty));

        public ArkApplication ArkApplication
        {
            get { return (ArkApplication)GetValue(ArkApplicationProperty); }
            set { SetValue(ArkApplicationProperty, value); }
        }

        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        public string Category
        {
            get { return (string)GetValue(CategoryProperty); }
            set { SetValue(CategoryProperty, value); }
        }

        public string DisplayName => GameData.FriendlyNameForClass(ClassName);

        public bool KnownItem => GameData.HasPrimalItemForClass(ClassName);

        public PrimalItem Duplicate()
        {
            var properties = this.GetType().GetProperties();

            var result = new PrimalItem();
            foreach (var prop in properties)
            {
                if (prop.CanWrite)
                    prop.SetValue(result, prop.GetValue(this));
            }

            return result;
        }
    }
}
